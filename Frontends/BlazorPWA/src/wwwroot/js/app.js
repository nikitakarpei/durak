window.hudTimer = {
    getElementSize: function (element) {
        if (!element) return { width: 0, height: 0 };
        return { width: element.clientWidth, height: element.clientHeight };
    }
};
