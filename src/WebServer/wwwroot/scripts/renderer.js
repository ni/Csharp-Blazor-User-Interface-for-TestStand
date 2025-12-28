window.openReport = async (filePath) => {
    try {
        const result = await window.electronAPI.viewReport(filePath);
        return result;
    } catch (error) {
        throw error;
    }
}

window.selectFile = async() => {
    try {
        const filePath = await window.electronAPI.browseFile();
        return filePath;
    } catch (error) {
        throw error;
    }
}

window.toggleTheme = async () => {
    try {
        await window.electronAPI.toggleTheme();
    } catch (error) {
        throw error;
    }
}

window.openUrlInBrowser = async (url) => {
    try {
        await window.electronAPI.openUrl(url);
    } catch (error) {
        throw error;
    }
}