// Emoji picker data and helper functions
window.emojiPicker = {
    // Common emoji categories
    categories: {
        "smileys": ["😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂", "🙂", "🙃", "😉", "😊", "😇", "🥰", "😍", "🤩", "😘", "😗", "😚", "😙"],
        "gestures": ["👋", "🤚", "🖐", "✋", "🖖", "👌", "🤌", "🤏", "✌️", "🤞", "🤟", "🤘", "🤙", "👈", "👉", "👆", "🖕", "👇", "☝️", "👍"],
        "hearts": ["❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💔", "❤️‍🔥", "❤️‍🩹", "💕", "💞", "💓", "💗", "💖", "💘", "💝", "💟"],
        "objects": ["⌚", "📱", "📲", "💻", "⌨️", "🖥", "🖨", "🖱", "🖲", "🕹", "🗜", "💾", "💿", "📀", "📼", "📷", "📸", "📹", "🎥", "📽"],
        "symbols": ["✅", "❌", "⭐", "🌟", "💫", "✨", "🔥", "💥", "💢", "💯", "💤", "💨", "🎉", "🎊", "🎈", "🎁", "🏆", "🥇", "🥈", "🥉"]
    },
    
    // Get all emojis as flat array
    getAll: function() {
        return Object.values(this.categories).flat();
    },
    
    // Search emojis by name (simplified - in production, use a proper emoji library)
    search: function(query) {
        // This is a simplified search - in production, use emoji-js or similar
        return this.getAll().filter(emoji => {
            // Basic search - you could enhance this with actual emoji names
            return emoji.includes(query) || query.length === 0;
        });
    }
};

// Simple markdown parser for chat messages
window.markdownParser = {
    parse: function(text) {
        if (!text) return '';
        
        // Escape HTML to prevent XSS
        let html = this.escapeHtml(text);
        
        // Bold: **text** or __text__
        html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/__(.+?)__/g, '<strong>$1</strong>');
        
        // Italic: *text* or _text_
        html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
        html = html.replace(/_(.+?)_/g, '<em>$1</em>');
        
        // Code: `code`
        html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
        
        // Code block: ```code```
        html = html.replace(/```([\s\S]+?)```/g, '<pre><code>$1</code></pre>');
        
        // Links: [text](url)
        html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');
        
        // Auto-link URLs
        html = html.replace(/(https?:\/\/[^\s]+)/g, '<a href="$1" target="_blank" rel="noopener noreferrer">$1</a>');
        
        // Mentions: @username
        html = html.replace(/@(\w+)/g, '<span class="mention">@$1</span>');
        
        // Convert newlines to <br>
        html = html.replace(/\n/g, '<br>');
        
        return html;
    },
    
    escapeHtml: function(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

// User color generator - generates consistent colors for usernames
window.userColors = {
    colors: [
        '#667eea', '#764ba2', '#f093fb', '#4facfe', '#43e97b',
        '#fa709a', '#fee140', '#30cfd0', '#a8edea', '#fed6e3',
        '#ff9a9e', '#fecfef', '#ffecd2', '#fcb69f', '#ff8a80',
        '#ff80ab', '#ea80fc', '#b388ff', '#8c9eff', '#82b1ff'
    ],
    
    getColor: function(username) {
        if (!username) return this.colors[0];
        
        let hash = 0;
        for (let i = 0; i < username.length; i++) {
            hash = username.charCodeAt(i) + ((hash << 5) - hash);
        }
        
        const index = Math.abs(hash) % this.colors.length;
        return this.colors[index];
    }
};

// Object URL helpers for image previews
window.createObjectURL = function(bytes, contentType) {
    try {
        const blob = new Blob([bytes], { type: contentType });
        return URL.createObjectURL(blob);
    } catch (error) {
        console.error('Error creating object URL:', error);
        return null;
    }
};

window.revokeObjectURL = function(url) {
    try {
        if (url) {
            URL.revokeObjectURL(url);
        }
    } catch (error) {
        console.error('Error revoking object URL:', error);
    }
};

