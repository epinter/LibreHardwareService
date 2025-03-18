using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Timers;
using static LibreHardwareService.ConfigHelper;

namespace LibreHardwareService;

public sealed class Service : IHostedService, IHostedLifecycleService {
    private readonly IHostEnvironment hostEnvironment;
    private readonly BackgroundWorker worker;
    private readonly SensorsManager sensorsManager;
    private readonly System.Timers.Timer timer;
    private bool stopping = false;
    private int interval = 1000;

    private bool debug = false;

    public bool IsDebug {
        get { return debug; }
        internal
            set {
            sensorsManager.IsDebug = true;
            debug = true;
        }
    }

    public Service(IHostEnvironment hostEnvironment, IHostApplicationLifetime applicationLifetime) {
        this.hostEnvironment = hostEnvironment;

        sensorsManager = new SensorsManager();
        worker = new BackgroundWorker();
        timer = new System.Timers.Timer();
        worker.WorkerSupportsCancellation = true;
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken) {
        onStarted();
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) {
        onStopping();
        return Task.CompletedTask;
    }

    Task IHostedLifecycleService.StartedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    Task IHostedLifecycleService.StartingAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    private void onStopping() {
        if (IsDebug) {
            Debug.WriteLine("{0}: Stopping LibreHardwareService", hostEnvironment.ApplicationName);
        }
        stopping = true;

        try {
            timer.Stop();
            timer.Dispose();
            worker?.CancelAsync();
            worker?.Dispose();
            sensorsManager.close();
        } catch (Exception) { }
    }

    private void onStarted() {
        startService();
        Log.info(hostEnvironment.ApplicationName + " started");
    }

    public void onTimeInterval(object? sender, ElapsedEventArgs args) {
        Debug.WriteLine("{0}: OnTimeInterval", hostEnvironment.ApplicationName);

        if (!worker.IsBusy && !stopping) {
            worker.RunWorkerAsync();
        }
    }

    private void updateSensors(object? sender, DoWorkEventArgs e) {
        Debug.WriteLine("{0}: UpdateSensors", hostEnvironment.ApplicationName);
        if (stopping) {
            return;
        }
        sensorsManager.updateHardwareSensors();
    }

    public void startService() {
        if (IsDebug) {
            Debug.WriteLine("{0}: DEBUG ENABLED, Interval set to {1}ms", hostEnvironment.ApplicationName, interval);
        } else {
            interval = readUpdateIntervalSetting();
        }

        worker.DoWork += updateSensors;

        worker.RunWorkerAsync();
        timer.Interval = interval;
        timer.Elapsed += new ElapsedEventHandler(onTimeInterval);
        timer.Enabled = true;
        Log.info(string.Format("Starting '{0}' version '{1}' with interval set to {2}ms, sensors time-window to {3} seconds",
                                hostEnvironment.ApplicationName,
                                Assembly.GetExecutingAssembly().GetName().Version,
                                interval,
                                sensorsManager.getSensorsTimeWindow().Seconds));
    }

    private int readUpdateIntervalSetting() {
        if (Config.UpdateIntervalMilliseconds > 250) {
            return Config.UpdateIntervalMilliseconds;
        }

        return interval;
    }
}