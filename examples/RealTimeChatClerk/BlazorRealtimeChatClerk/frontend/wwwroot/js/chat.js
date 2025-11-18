// Chat UI helper functions
window.scrollToBottom = (element) => {
    if (!element) return;
    
    // Find the scrollable container (messages-container)
    let container = element;
    if (container.classList && container.classList.contains('messages-container')) {
        // Use it directly
    } else {
        // Try to find parent with messages-container class
        let current = element.parentElement;
        while (current && current !== document.body) {
            if (current.classList && current.classList.contains('messages-container')) {
                container = current;
                break;
            }
            current = current.parentElement;
        }
        // If not found, try to find the scrollable parent
        if (!container.classList || !container.classList.contains('messages-container')) {
            container = element.closest('.messages-container') || element.parentElement || element;
        }
    }
    
    // Scroll the container to the bottom smoothly
    if (container && container.scrollHeight) {
        container.scrollTo({
            top: container.scrollHeight,
            behavior: 'smooth'
        });
    } else if (element && element.scrollIntoView) {
        // Fallback: scroll element into view
        element.scrollIntoView({ behavior: 'smooth', block: 'end' });
    }
};

window.isNearBottom = (containerElement, threshold = 100) => {
    if (!containerElement) return false;
    // Find the scrollable container - it should be the messages-container
    let container = containerElement;
    // Try to find the messages-container parent or use the element itself if it has the class
    if (container.classList && container.classList.contains('messages-container')) {
        // Use it directly
    } else {
        // Try to find parent with messages-container class
        let current = container.parentElement;
        while (current && current !== document.body) {
            if (current.classList && current.classList.contains('messages-container')) {
                container = current;
                break;
            }
            current = current.parentElement;
        }
        // If not found, use the element itself or its parent
        if (!container.classList || !container.classList.contains('messages-container')) {
            container = containerElement.parentElement || containerElement;
        }
    }
    const scrollTop = container.scrollTop || 0;
    const scrollHeight = container.scrollHeight || 0;
    const clientHeight = container.clientHeight || 0;
    const distanceFromBottom = scrollHeight - scrollTop - clientHeight;
    return distanceFromBottom <= threshold;
};

window.focusElement = (element) => {
    if (element && element.focus) {
        element.focus();
    }
};

window.scrollToElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
};

window.copyToClipboard = async (text) => {
    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(text);
        } else {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.opacity = '0';
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
        }
    } catch (err) {
        console.error('Failed to copy text:', err);
        throw err;
    }
};

// Get IDs of messages currently visible in the viewport
window.getVisibleMessageIds = () => {
    const messageIds = [];
    const messages = document.querySelectorAll('[id^="message-"]');
    const viewportTop = window.scrollY || window.pageYOffset;
    const viewportBottom = viewportTop + window.innerHeight;
    
    messages.forEach(message => {
        const rect = message.getBoundingClientRect();
        const messageTop = rect.top + viewportTop;
        const messageBottom = messageTop + rect.height;
        
        // Check if message is visible in viewport (with some padding)
        if (messageBottom >= viewportTop && messageTop <= viewportBottom) {
            const id = message.id.replace('message-', '');
            if (id) {
                messageIds.push(id);
            }
        }
    });
    
    return messageIds;
};

