// Clerk JavaScript interop - simplified version
// This file provides simple wrapper functions for Clerk SDK interaction from Blazor

// Function to load Clerk SDK dynamically
window.clerk = {
  // Load Clerk SDK script with publishable key to prevent auto-init error
  loadScript: function (url, publishableKey) {
    return new Promise((resolve, reject) => {
      // Check if script is already loaded
      if (window.Clerk) {
        resolve();
        return;
      }

      // Set publishable key in a way Clerk can find it before loading
      // This prevents the auto-init error
      if (publishableKey) {
        // Store key globally
        window.__clerk_publishable_key = publishableKey;
        // Set on document element
        document.documentElement.setAttribute(
          "data-clerk-publishable-key",
          publishableKey
        );
      }

      // Set up comprehensive error suppression
      const originalError = window.onerror;
      const originalConsoleError = console.error;
      const originalConsoleWarn = console.warn;

      const errorHandler = function (msg, url, line, col, error) {
        const msgStr = String(msg || "");
        if (
          msgStr.includes("Missing publishableKey") ||
          msgStr.includes("@clerk/clerk-js") ||
          msgStr.includes("publishableKey")
        ) {
          return true; // Suppress
        }
        if (originalError) {
          return originalError.apply(this, arguments);
        }
        return false;
      };

      window.onerror = errorHandler;

      // Also catch unhandled promise rejections
      window.addEventListener("unhandledrejection", function (event) {
        const reason = String(event.reason || "");
        if (
          reason.includes("Missing publishableKey") ||
          reason.includes("@clerk/clerk-js")
        ) {
          event.preventDefault();
          return;
        }
      });

      console.error = function (...args) {
        const msg = args.map((a) => String(a)).join(" ");
        if (
          msg.includes("Missing publishableKey") ||
          msg.includes("@clerk/clerk-js") ||
          msg.includes("publishableKey")
        ) {
          return; // Suppress
        }
        originalConsoleError.apply(console, args);
      };

      console.warn = function (...args) {
        const msg = args.map((a) => String(a)).join(" ");
        if (
          msg.includes("Missing publishableKey") ||
          msg.includes("@clerk/clerk-js") ||
          msg.includes("publishableKey")
        ) {
          return; // Suppress
        }
        originalConsoleWarn.apply(console, args);
      };

      const script = document.createElement("script");

      // Set data attribute BEFORE setting src (Clerk checks this during load)
      if (publishableKey) {
        script.setAttribute("data-clerk-publishable-key", publishableKey);
      }

      script.src = url;
      // Use defer instead of async to ensure script executes after DOM is ready
      script.defer = true;

      script.onload = () => {
        // Wait for Clerk to be available
        let attempts = 0;
        const checkClerk = setInterval(() => {
          if (window.Clerk) {
            clearInterval(checkClerk);
            // Restore error handlers after a delay
            setTimeout(() => {
              window.onerror = originalError;
              console.error = originalConsoleError;
              console.warn = originalConsoleWarn;
            }, 200);
            resolve();
          } else if (attempts++ > 50) {
            clearInterval(checkClerk);
            // Restore error handlers
            window.onerror = originalError;
            console.error = originalConsoleError;
            console.warn = originalConsoleWarn;
            reject(new Error("Clerk SDK not available after loading script"));
          }
        }, 100);
      };

      script.onerror = () => {
        // Restore error handlers on error
        window.onerror = originalError;
        console.error = originalConsoleError;
        console.warn = originalConsoleWarn;
        reject(new Error("Failed to load Clerk SDK script"));
      };

      // Append script - this will trigger loading
      document.head.appendChild(script);
    });
  },
  // Initialize Clerk with publishable key
  initialize: async function (publishableKey) {
    if (!window.Clerk) {
      throw new Error(
        "Clerk SDK not loaded. Make sure clerk.browser.js is loaded before calling initialize."
      );
    }

    // Check if Clerk is already initialized
    if (
      window.Clerk.loaded ||
      (window.Clerk.user !== undefined && window.Clerk.user !== null)
    ) {
      // Already initialized, just verify it's working
      return;
    }

    if (typeof window.Clerk.load === "function") {
      try {
        await window.Clerk.load({ publishableKey });
      } catch (error) {
        // If load fails, Clerk might already be initialized
        if (error.message && error.message.includes("already")) {
          return; // Already initialized
        }
        throw error;
      }
    } else if (typeof window.Clerk === "function") {
      window.Clerk = new window.Clerk({ publishableKey });
      await window.Clerk.load();
    } else {
      throw new Error("Clerk SDK is not in the expected format");
    }
  },

  // Get authentication token from Clerk
  getToken: async function (template, skipCache) {
    if (!window.Clerk) {
      console.warn("Clerk SDK not loaded");
      return null;
    }
    
    // Wait for Clerk to be ready
    if (window.Clerk.load && !window.Clerk.loaded) {
      try {
        await window.Clerk.load();
      } catch (error) {
        console.warn("Clerk already loading or loaded:", error);
      }
    }
    
    // Check if user is authenticated
    if (!window.Clerk.user) {
      console.warn("Clerk user not authenticated");
      return null;
    }
    
    // Get session - Clerk.session might be a getter
    const session = window.Clerk.session || (window.Clerk.user && window.Clerk.user.sessions && window.Clerk.user.sessions[0]);
    
    if (!session) {
      console.warn("Clerk session not available");
      return null;
    }
    
    try {
      // Clerk's getToken API: session.getToken(template, options) or session.getToken({ template, ... })
      if (typeof session.getToken === "function") {
        // Try with options object first (newer API)
        const options = { template: template || "convex" };
        if (skipCache !== undefined) {
          options.skipCache = skipCache;
        }
        const token = await session.getToken(options);
        return token;
      } else {
        console.error("session.getToken is not a function");
        return null;
      }
    } catch (error) {
      console.error("Error getting Clerk token:", error);
      // Try alternative API format
      try {
        const token = await session.getToken(template || "convex");
        return token;
      } catch (altError) {
        console.error("Alternative getToken call also failed:", altError);
        return null;
      }
    }
  },

  // Check if user is signed in
  isSignedIn: function () {
    return window.Clerk && window.Clerk.user !== null;
  },

  // Get user ID
  getUserId: function () {
    if (window.Clerk && window.Clerk.user) {
      return window.Clerk.user.id || null;
    }
    return null;
  },

  // Get user email
  getUserEmail: function () {
    if (
      window.Clerk &&
      window.Clerk.user &&
      window.Clerk.user.emailAddresses &&
      window.Clerk.user.emailAddresses.length > 0
    ) {
      return window.Clerk.user.emailAddresses[0].emailAddress || null;
    }
    return null;
  },

  // Sign out
  signOut: function () {
    if (window.Clerk && typeof window.Clerk.signOut === "function") {
      return window.Clerk.signOut();
    }
    return Promise.resolve();
  },

  // Open sign-in modal
  openSignIn: function () {
    if (window.Clerk && typeof window.Clerk.openSignIn === "function") {
      window.Clerk.openSignIn();
    }
  },

  // Set up listener for auth state changes
  addListener: function (dotNetHelper, methodName) {
    if (window.Clerk && typeof window.Clerk.addListener === "function") {
      window.Clerk.addListener((state) => {
        if (dotNetHelper && window.DotNet && window.DotNet.invokeMethodAsync) {
          window.DotNet.invokeMethodAsync(
            dotNetHelper,
            methodName,
            state.user !== null
          ).catch((err) => console.error("Error invoking Blazor method:", err));
        }
      });
    }
  },
};

