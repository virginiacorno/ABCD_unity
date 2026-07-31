mergeInto(LibraryManager.library, {
    SendDataToJS: function (jsonDataPtr) {
        var jsonData = UTF8ToString(jsonDataPtr);
        try {
            var data = JSON.parse(jsonData);
            if (window.parent && window.parent !== window) {
                // Route through the shared dedup gate defined in index.html,
                // rather than posting directly — this is what was causing
                // events to be delivered twice (once from here, once from
                // the [WEBGL_DATA] Debug.Log path picked up by index.html's
                // console interceptor).
                if (typeof window.forwardUnityDataOnce === 'function') {
                    window.forwardUnityDataOnce(data);
                } else {
                    // Fallback in case index.html's gate isn't present for some reason
                    window.parent.postMessage({ type: 'UNITY_DATA', data: data }, '*');
                }
            }
            console.log('[Unity -> JS]:', data);
        } catch (e) {
            console.error('Failed to parse Unity data:', e);
        }
    }
});