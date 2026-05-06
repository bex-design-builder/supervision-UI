mergeInto(LibraryManager.library, {
    GetKeyboardHeightFraction: function () {
        if (!window.visualViewport) return 0;
        var kbHeight = window.innerHeight - window.visualViewport.height;
        return kbHeight > 10 ? kbHeight / window.innerHeight : 0;
    }
});
