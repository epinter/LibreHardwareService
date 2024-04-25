using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using static LibreHardwareService.ConfigHelper;

namespace LibreHardwareService;

public sealed class Service : IHostedService, IHostedLifecycleService {
    private readonly IHostEnvironment hostEnvironment;
    private readonly BackgroundWorker worker;
    private readonly SensorsManager sensorsManager;
    private readonly System.Timers.Timer timer;
    private int interval = 1000;

    private bool debug = false;

    public bool isDebug {
        get { return debug; }
    internal
        set {
            sensorsManager.isDebug = true;
            debug = true;
        }
    }

    public Service(IHostEnvironment hostEnvironment, IHostApplicationLifetime applicationLifetime) {
        this.hostEnvironment = hostEnvironment;
        applicationLifetime.ApplicationStarted.Register(onStarted);
        // applicationLifetime.ApplicationStopping.Register(OnStopping);
        applicationLifetime.ApplicationStopped.Register(onStopped);

        sensorsManager = new SensorsManager();
        worker = new BackgroundWorker();
        timer = new System.Timers.Timer();
        worker.WorkerSupportsCancellation = true;
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) {
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
    private void onStopped() {
        Log.info(hostEnvironment.ApplicationName + " stopped");
        if (isDebug) {
            Debug.WriteLine("{0}: Stopping LibreHardwareService", hostEnvironment.ApplicationName);
        }

        sensorsManager.close();
        timer.Stop();
        timer.Dispose();
        worker?.CancelAsync();
        worker?.Dispose();
    }

    private void onStarted() {
        startService();
        Log.info(hostEnvironment.ApplicationName + " started");
    }

    public void onTimeInterval(object? sender, ElapsedEventArgs args) {
        Debug.WriteLine("{0}: OnTimeInterval", hostEnvironment.ApplicationName);

        if (!worker.IsBusy) {
            worker.RunWorkerAsync();
        }
    }

    private void updateSensors(object? sender, DoWorkEventArgs e) {
        Debug.WriteLine("{0}: UpdateSensors", hostEnvironment.ApplicationName);

        sensorsManager.updateHardwareSensors();
    }

    public void startService() {
        if (isDebug) {
            Debug.WriteLine("{0}: DEBUG ENABLED, Interval set to {1}ms", hostEnvironment.ApplicationName, interval);
        } else {
            interval = readUpdateIntervalSetting();
        }

        worker.DoWork += updateSensors;

        worker.RunWorkerAsync();
        timer.Interval = interval;
        timer.Elapsed += new ElapsedEventHandler(onTimeInterval);
        timer.Enabled = true;
        Log.info(String.Format("Starting '{0}' with interval set to {1}ms, sensors time-window to {2} minutes",
                               hostEnvironment.ApplicationName, interval, sensorsManager.getSensorsTimeWindow()));
    }

    private int readUpdateIntervalSetting() {
        if (Config.UpdateIntervalMilliseconds > 250) {
            return Config.UpdateIntervalMilliseconds;
        }

        return interval;
    }
}