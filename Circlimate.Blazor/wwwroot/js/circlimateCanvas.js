window.circlimateCanvas = {
    backgroundCanvas: null,
    foregroundCanvas: null,
    backgroundCtx: null,
    foregroundCtx: null,

    initialize: function(backgroundCanvasId, foregroundCanvasId) {
        this.backgroundCanvas = document.getElementById(backgroundCanvasId);
        this.foregroundCanvas = document.getElementById(foregroundCanvasId);

        if (!this.backgroundCanvas || !this.foregroundCanvas) {
            console.error('Canvas elements not found');
            return false;
        }

        this.backgroundCtx = this.backgroundCanvas.getContext('2d');
        this.foregroundCtx = this.foregroundCanvas.getContext('2d');

        console.log('Canvas initialized successfully');
        return true;
    },

    clearAll: function() {
        if (this.backgroundCtx && this.foregroundCtx) {
            this.backgroundCtx.clearRect(0, 0, 800, 800);
            this.foregroundCtx.clearRect(0, 0, 800, 800);
        }
    },

    fadeBackgroundAndTransfer: function(fadeAmount) {
        if (!this.backgroundCtx || !this.foregroundCtx) return;

        // Copy foreground to background with fade
        this.backgroundCtx.globalAlpha = fadeAmount; // e.g., 0.85 = retain 85%
        this.backgroundCtx.drawImage(this.foregroundCanvas, 0, 0);
        this.backgroundCtx.globalAlpha = 1.0;

        // Clear foreground for new year
        this.foregroundCtx.clearRect(0, 0, 800, 800);
    },

    drawDots: function(dotsData) {
        if (!this.foregroundCtx) return;

        // dotsData: [{ x, y, color, radius }]
        for (let dot of dotsData) {
            this.foregroundCtx.beginPath();
            this.foregroundCtx.arc(dot.x, dot.y, dot.radius, 0, 2 * Math.PI);
            this.foregroundCtx.fillStyle = dot.color;
            this.foregroundCtx.fill();
        }
    }
};
