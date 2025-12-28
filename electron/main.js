const { app, Menu, BrowserWindow, ipcMain, shell, dialog, screen } = require('electron');
const path = require('node:path');
const fs = require('fs');
const http = require('http');
const { spawn } = require('child_process');
const { nativeTheme } = require('electron/main');

let serverProcess = null;
let mainWindow = null;
const serverProcessName = "NationalInstruments.TestStand.WebOI.WebServer.exe";
const appDataBasePath = path.join(app.getPath('appData'), 'National Instruments', 'TestStand WebOI');
const urlFilePath = path.join(appDataBasePath, 'webserver-url.txt');
const logFilePath = path.join(appDataBasePath, 'logs', 'app.log');
const MAX_LOG_SIZE = 5 * 1024 * 1024; // 5 MB

// set dark theme as default
nativeTheme.themeSource = 'dark';

function logToFile(message) {
    try {
        fs.mkdirSync(path.dirname(logFilePath), { recursive: true });
        fs.appendFileSync(logFilePath, `[${new Date().toISOString()}] ${message}\n`);
        const stats = fs.statSync(logFilePath);
        if (stats.size > MAX_LOG_SIZE) {
            const archivePath = logFilePath.replace('.log', `-${Date.now()}.log`);
            fs.renameSync(logFilePath, archivePath);
            cleanupOldLogFiles();
        }
    } catch (err) {
        console.error('Failed to write to log file:', err.message);
    }
}

function cleanupOldLogFiles() {
    try {
        const logDir = path.dirname(logFilePath);
        const logFileName = path.basename(logFilePath, '.log');
        const files = fs.readdirSync(logDir);

        // Filter archived log files matching the pattern: app-<timestamp>.log
        const archivedLogs = files
            .filter(file => file.startsWith(`${logFileName}-`) && file.endsWith('.log'))
            .map(file => ({
                name: file,
                path: path.join(logDir, file),
                time: fs.statSync(path.join(logDir, file)).mtime.getTime()
            }))
            .sort((a, b) => b.time - a.time); // Sort by modification time, newest first

        // Keep only the 10 most recent archived logs
        const logsToDelete = archivedLogs.slice(10);
        logsToDelete.forEach(log => {
            try {
                fs.unlinkSync(log.path);
                console.log(`Deleted old log file: ${log.name}`);
            } catch (err) {
                console.error(`Failed to delete log file ${log.name}:`, err.message);
            }
        });
    } catch (err) {
        console.error('Failed to cleanup old log files:', err.message);
    }
}

const createWindow = async () => {
    const iconPath = app.isPackaged
        ? path.join(process.resourcesPath, 'assets', 'teststand_64bit.ico')
        : path.join(__dirname, 'assets', 'teststand_64bit.ico');
    const { width, height } = screen.getPrimaryDisplay().workAreaSize;
    try {
        mainWindow = new BrowserWindow({
            width: width,
            height: height,
            icon: iconPath,
            webPreferences: {
                contextIsolation: true,
                sandbox: true,
                nodeIntegration: false,
                preload: path.join(__dirname, 'preload.js')
            }
        });
    } catch (err) {
        logToFile(`Failed to create BrowserWindow: ${err.message}`);
        return;
    }

    mainWindow.loadFile(path.join(__dirname, 'loader.html'));

    let webServerUrl;
    try {
        webServerUrl = await getWebServerUrl();
        logToFile(`Loading Url: ${webServerUrl}`);
        mainWindow.loadURL(webServerUrl);
        mainWindow.webContents.on('did-finish-load', () => {
            // Set zoom factor to 1.05 to ensure UI elements are properly scaled and readable
            mainWindow.webContents.setZoomFactor(1.05);
        });
    } catch (err) {
        logToFile(`Error occurred while getting the server url: ${err.message}`);
        mainWindow.loadFile(path.join(__dirname, 'error.html'));
    }
};

const startServer = () => {
    let exePath;
    try {
        if (process.env.NODE_ENV === 'development') {
            exePath = path.join(__dirname, '..', 'Tests', 'publishWin', 'Debug', serverProcessName);
            logToFile('Running in development mode');
        } else {
            exePath = path.join(process.resourcesPath, 'app', serverProcessName);
        }
        logToFile(`Starting server from: ${exePath}`);
        serverProcess = spawn(exePath, [], {
            cwd: path.dirname(exePath)
        });
        serverProcess.on('error', (err) => {
            logToFile(`Server process failed to start: ${err.message}`);
        });
        serverProcess.on('exit', (code, signal) => {
            logToFile(`Server process exited with code: ${code}, signal: ${signal}`);
        });
        logToFile(`Server process started with PID: ${serverProcess.pid}`);
    } catch (err) {
        logToFile(`Failed to start server process: ${err.message}`);
    }
};

const quit = () => {
    try {
        if (serverProcess) serverProcess.kill();
    } catch (err) {
        logToFile(`Failed to kill server process: ${err.message}`);
    }
    try {
        if (fs.existsSync(urlFilePath)) {
            fs.unlinkSync(urlFilePath);
        }
    } catch (err) {
        logToFile(`Failed to delete url file: ${err.message}`);
    }
    app.quit();
};

// Waits for the webserver-url.txt file, reads the URL, verifies it is working, and returns the URL
async function getWebServerUrl(options = {}) {
    const timeoutMs = options.timeoutMs || 30000;               // max wait time
    const pollIntervalMs = options.pollIntervalMs || 500;       // polling interval
    const verifyTimeoutMs = options.verifyTimeoutMs || 5000;    // timeout for url verification

    function verifyUrl(url) {
        return new Promise((resolve) => {
            try {
                const req = http.get(url, { timeout: verifyTimeoutMs }, (res) => {
                    resolve(res.statusCode >= 200 && res.statusCode < 400);
                    res.resume();
                });
                req.on('error', () => resolve(false));
                req.on('timeout', () => {
                    req.destroy();
                    resolve(false);
                });
            } catch (err) {
                logToFile(`Error verifying URL: ${err.message}`);
                resolve(false);
            }
        });
    }

    return new Promise((resolve, reject) => {
        const start = Date.now();
        const poll = async () => {
            try {
                if (fs.existsSync(urlFilePath)) {
                    const url = fs.readFileSync(urlFilePath, 'utf-8').trim();
                    if (await verifyUrl(url)) {
                        console.log(`URL is responding: ${url}`);
                        return resolve(url);
                    }
                    console.log(`URL not responding yet: ${url}`);
                }

                if (Date.now() - start > timeoutMs) {
                    return reject(new Error('Web server URL file not found or URL not responding in time.'));
                }
                setTimeout(poll, pollIntervalMs);
            } catch (err) {
                logToFile(`Error in getWebServerUrl polling: ${err.message}`);
                setTimeout(poll, pollIntervalMs);
            }
        };
        poll();
    });
}

function isSafeReportFile(filePath) {
    const allowedExtensions = ['.pdf', '.html', '.txt', '.csv', '.xml'];
    const ext = path.extname(filePath).toLowerCase();
    return allowedExtensions.includes(ext);

}
// Opens the report file in the default external application.
async function viewReport(event, reportLocation) {
    try {
        reportLocation = path.normalize(reportLocation);
        if (!isSafeReportFile(reportLocation)) {
            const message = `Blocked unsafe file type: ${reportLocation}`;
            logToFile(message);
            throw new Error(message);
        }
        if (fs.existsSync(reportLocation)) {
            shell.openPath(reportLocation);
        } else {
            const message = `Report not found in the given location: ${reportLocation}`;
            logToFile(message);
            throw new Error(message);
        }
    } catch (error) {
        const message = `Error opening report at ${reportLocation}: ${error.message}`;
        logToFile(message);
        throw new Error(message);
    }
}

// open a file dialog to select a file and return the selected file path
async function browseFile(event) {
    try {
        const result = await dialog.showOpenDialog({
            properties: ['openFile'],
            filters: [
                { name: 'TestStand Files', extensions: ["seq"] }
            ]
        });
        if (!result.canceled && result.filePaths.length > 0) {
            return result.filePaths[0];
        }
    } catch (error) {
        const message = `Failed to open file dialog: ${error.message}`;
        logToFile(message);
        throw new Error(message);
    }
    return null;
}

function toggleTheme() {
    nativeTheme.themeSource = nativeTheme.themeSource == 'dark' ? 'light' : 'dark';
}

function openUrlInDefaultBrowser(event, url) {
    shell.openExternal(url).catch((err) => {
        logToFile(`Failed to open URL ${url} in default browser: ${err.message}`);
    });
}

const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
    app.quit();
}
else {
    app.on('second-instance', () => {
        if (mainWindow) {
            if (mainWindow.isMinimized()) mainWindow.restore();
            mainWindow.focus();
        }
    });
}

Menu.setApplicationMenu(null);
app.whenReady().then(async () => {
    ipcMain.handle('view-report', viewReport);
    ipcMain.handle('browseFile', browseFile);
    ipcMain.handle('toggle-theme', toggleTheme);
    ipcMain.handle('open-url', openUrlInDefaultBrowser);
    startServer();
    await createWindow();
});

app.on('window-all-closed', quit);
app.on('will-quit', quit);