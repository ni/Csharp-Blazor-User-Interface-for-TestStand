const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
    browseFile: async () => await ipcRenderer.invoke('browseFile'),
    viewReport: async (filePath) => await ipcRenderer.invoke('view-report', filePath),
    toggleTheme: async () => await ipcRenderer.invoke('toggle-theme')
})
