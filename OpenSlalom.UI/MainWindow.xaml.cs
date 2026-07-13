using Microsoft.EntityFrameworkCore;
using NLog;
using OpenSlalom.Data;
using OpenSlalom.Data.Entities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OpenSlalom.UI;

public partial class MainWindow : Window
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly Brush ActiveMenuBackgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F84DE"));
    private static readonly Brush DefaultMenuBackgroundBrush = Brushes.Transparent;
    private static readonly Brush ActiveMenuForegroundBrush = Brushes.White;
    private static readonly Brush DefaultMenuForegroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
    private static readonly Color MenuFallbackBackgroundColor = Colors.White;
    private const double HoverDarkenFactor = 0.15;
    private const string TrainingDetailTagPrefix = "TrainingDetail:";

    private readonly IDbContextFactory<LocalOpenSlalomDbContext> _localDbContextFactory;
    private readonly DataSyncService _dataSyncService;
    private readonly DatabaseRuntimeInfo _databaseRuntimeInfo;
    private readonly string _uiSettingsFilePath;
    private string _selectedMenuTag = "Startseite";
    private bool _syncInProgress;
    private bool _syncNeeded;
    private int? _editDisziplinId;
    private int? _deleteDisziplinId;
    private int? _editVereinId;
    private int? _deleteVereinId;
    private int? _editFahrerId;
    private int? _deleteFahrerId;
    private int? _editKartId;
    private int? _deleteKartId;
    private int? _editWetterId;
    private int? _deleteWetterId;
    private int? _editTrainingId;
    private int? _deleteTrainingId;
    private int? _selectedTrainingDetailId;
    private string _trainingDriverSearchTerm = string.Empty;
    private int _trainingDriverSelectionOrderCounter;
    private readonly List<Button> _dynamicTrainingMenuButtons = [];
    private readonly Dictionary<int, int> _trainingActiveDriverByTrainingId = new();
    private readonly Dictionary<(int TrainingId, int FahrerId), bool> _trainingDriverEnabledByDriver = new();
    private readonly Dictionary<(int TrainingId, int FahrerId), int?> _trainingKartSelectionByDriver = new();
    private readonly DispatcherTimer _trainingStopwatchTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Dictionary<(int TrainingId, int FahrerId), TrainingStintState> _trainingStintsByDriver = new();
    private (int TrainingId, int FahrerId)? _trainingStopwatchContext;
    private bool _nextDriverSwitchInProgress;
    private bool _finishTrainingInProgress;
    private double _selectedTrainingTorfehlerPenaltySeconds;
    private double _selectedTrainingPylonenfehlerPenaltySeconds;
    private LocalUiSettings _localUiSettings = new();

    public ObservableCollection<DisziplinListItem> DisziplinItems { get; } = new();
    public ObservableCollection<VereinListItem> VereineItems { get; } = new();
    public ObservableCollection<FahrerListItem> FahrerItems { get; } = new();
    public ObservableCollection<KartListItem> KartItems { get; } = new();
    public ObservableCollection<WetterListItem> WetterItems { get; } = new();
    public ObservableCollection<TrainingListItem> TrainingItems { get; } = new();
    public ObservableCollection<TrainingStarterListItem> TrainingStarterListItems { get; } = new();
    public ObservableCollection<TrainingLapTimeListItem> TrainingLapTimeItems { get; } = new();
    public ObservableCollection<TrainingFastestLapListItem> TrainingFastestLapItems { get; } = new();
    public ObservableCollection<TrainingDriverSelectionItem> TrainingDriverSelectionItems { get; } = new();
    public ObservableCollection<LookupItem> KartLookupItems { get; } = new();
    public ObservableCollection<LookupItem> VereinLookupItems { get; } = new();
    public ObservableCollection<LookupItem> DisziplinLookupItems { get; } = new();
    public ObservableCollection<LookupItem> WetterLookupItems { get; } = new();

    public MainWindow(
        IDbContextFactory<LocalOpenSlalomDbContext> localDbContextFactory,
        DataSyncService dataSyncService,
        DatabaseRuntimeInfo databaseRuntimeInfo)
    {
        _localDbContextFactory = localDbContextFactory;
        _dataSyncService = dataSyncService;
        _databaseRuntimeInfo = databaseRuntimeInfo;
        _uiSettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenSlalom",
            "ui-settings.json");
        InitializeComponent();
        DataContext = this;
        ConfigureMenuButtons();
        SetVersionText();
        _trainingStopwatchTimer.Tick += TrainingStopwatchTimer_OnTick;
        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        NavigateTo("Startseite");
        StateChanged += OnWindowStateChanged;
        Loaded += OnLoaded;
        UpdateWindowChrome();
    }

    private void SetVersionText()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unbekannt";
        FooterVersionTextBlock.Text = $"Version {version}";
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigateTo("Startseite");
        await LoadLocalUiSettingsAsync();
        await LoadTrainingsAsync();
        await LoadDisziplinenAsync();
        await LoadVereineAsync();
        await LoadFahrerAsync();
        await LoadKartsAsync();
        await LoadWetterAsync();
        await LoadLookupDataAsync();
        UpdateConnectionStatus();
        await RefreshSyncStatusAsync();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        UpdateWindowChrome();
    }

    private void UpdateWindowChrome()
    {
        WindowRootBorder.Margin = new Thickness(0);
        WindowRootBorder.CornerRadius = new CornerRadius(0);
    }

    private async Task LoadVereineAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var vereine = await dbContext.Vereine
                .AsNoTracking()
                .OrderBy(x => x.Vereinsname)
                .Select(x => new VereinListItem
                {
                    Id = x.Id,
                    Vereinsname = x.Vereinsname,
                    MitgliedsNummer = x.MitgliedsNummer
                })
                .ToListAsync();

            VereineItems.Clear();
            foreach (var verein in vereine)
            {
                VereineItems.Add(verein);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Vereine aus SQLite.");
        }
    }

    private async Task LoadDisziplinenAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var disziplinen = await dbContext.Disziplinen
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new DisziplinListItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    ZeitstrafeTorfehler = x.ZeitstrafeTorfehler,
                    ZeitstrafePylonenfehler = x.ZeitstrafePylonenfehler,
                    ZeitstrafeTorfehlerText = FormatSecondsValue(x.ZeitstrafeTorfehler),
                    ZeitstrafePylonenfehlerText = FormatSecondsValue(x.ZeitstrafePylonenfehler)
                })
                .ToListAsync();

            DisziplinItems.Clear();
            foreach (var item in disziplinen)
            {
                DisziplinItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Disziplinen aus SQLite.");
        }
    }

    private async Task LoadFahrerAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var fahrer = await dbContext.Fahrer
                .AsNoTracking()
                .Include(x => x.Verein)
                .OrderBy(x => x.Vorname)
                .ThenBy(x => x.Nachname)
                .Select(x => new FahrerListItem
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    Vorname = x.Vorname,
                    Nachname = x.Nachname ?? string.Empty,
                    Geburtsdatum = x.Geburtsdatum,
                    GeburtsdatumText = x.Geburtsdatum.HasValue ? x.Geburtsdatum.Value.ToString("dd.MM.yyyy") : string.Empty,
                    VereinName = x.Verein.Vereinsname
                })
                .ToListAsync();

            FahrerItems.Clear();
            foreach (var item in fahrer)
            {
                FahrerItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Fahrer aus SQLite.");
        }
    }

    private async Task LoadKartsAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var karts = await dbContext.Karts
                .AsNoTracking()
                .Include(x => x.Verein)
                .Include(x => x.Disziplin)
                .OrderBy(x => x.Name)
                .Select(x => new KartListItem
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    DisziplinId = x.DisziplinId,
                    Name = x.Name ?? string.Empty,
                    Motor = x.Motor ?? string.Empty,
                    Chassis = x.Chassis ?? string.Empty,
                    VereinName = x.Verein.Vereinsname,
                    DisziplinName = x.Disziplin.Name
                })
                .ToListAsync();

            KartItems.Clear();
            foreach (var item in karts)
            {
                KartItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Karts aus SQLite.");
        }
    }

    private async Task LoadWetterAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var wetter = await dbContext.Wetterlagen
                .AsNoTracking()
                .OrderBy(x => x.Bezeichnung)
                .Select(x => new WetterListItem
                {
                    Id = x.Id,
                    Name = x.Bezeichnung
                })
                .ToListAsync();

            WetterItems.Clear();
            foreach (var item in wetter)
            {
                WetterItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Wetterdaten aus SQLite.");
        }
    }

    private async Task LoadTrainingsAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var trainings = await dbContext.Trainings
                .AsNoTracking()
                .Include(x => x.Verein)
                .Include(x => x.Disziplin)
                .Include(x => x.Wetter)
                .OrderByDescending(x => x.Zeitpunkt)
                .ThenBy(x => x.Name)
                .Select(x => new TrainingListItem
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    DisziplinId = x.DisziplinId,
                    WetterId = x.WetterId,
                    Name = x.Name,
                    Beschreibung = x.Beschreibung,
                    Zeitpunkt = x.Zeitpunkt,
                    ZeitpunktText = x.Zeitpunkt.ToString("dd.MM.yyyy"),
                    TrainingAbgeschlossen = x.TrainingAbgeschlossen,
                    TrainingAbgeschlossenText = x.TrainingAbgeschlossen ? "Ja" : "Nein",
                    VereinName = x.Verein.Vereinsname,
                    DisziplinName = x.Disziplin.Name,
                    WetterName = x.Wetter.Bezeichnung
                })
                .ToListAsync();

            TrainingItems.Clear();
            foreach (var item in trainings)
            {
                TrainingItems.Add(item);
            }

            RefreshOpenTrainingMenuButtons();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Trainings aus SQLite.");
        }
    }

    private async Task LoadLookupDataAsync()
    {
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

        var vereine = await dbContext.Vereine
            .AsNoTracking()
            .OrderBy(x => x.Vereinsname)
            .Select(x => new LookupItem { Id = x.Id, Name = x.Vereinsname })
            .ToListAsync();

        var disziplinen = await dbContext.Disziplinen
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem { Id = x.Id, Name = x.Name })
            .ToListAsync();

        var wetter = await dbContext.Wetterlagen
            .AsNoTracking()
            .OrderBy(x => x.Bezeichnung)
            .Select(x => new LookupItem { Id = x.Id, Name = x.Bezeichnung })
            .ToListAsync();

        var karts = await dbContext.Karts
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new LookupItem
            {
                Id = x.Id,
                Name = string.IsNullOrWhiteSpace(x.Name) ? $"Kart #{x.Id}" : x.Name
            })
            .ToListAsync();

        VereinLookupItems.Clear();
        foreach (var item in vereine)
        {
            VereinLookupItems.Add(item);
        }

        DisziplinLookupItems.Clear();
        foreach (var item in disziplinen)
        {
            DisziplinLookupItems.Add(item);
        }

        WetterLookupItems.Clear();
        foreach (var item in wetter)
        {
            WetterLookupItems.Add(item);
        }

        KartLookupItems.Clear();
        foreach (var item in karts)
        {
            KartLookupItems.Add(item);
        }

        CreateFahrerVereinComboBox.ItemsSource = VereinLookupItems;
        EditFahrerVereinComboBox.ItemsSource = VereinLookupItems;
        CreateKartVereinComboBox.ItemsSource = VereinLookupItems;
        EditKartVereinComboBox.ItemsSource = VereinLookupItems;
        CreateKartDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        EditKartDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        CreateTrainingVereinComboBox.ItemsSource = VereinLookupItems;
        EditTrainingVereinComboBox.ItemsSource = VereinLookupItems;
        CreateTrainingDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        EditTrainingDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        CreateTrainingWetterComboBox.ItemsSource = WetterLookupItems;
        EditTrainingWetterComboBox.ItemsSource = WetterLookupItems;
    }

    private async Task LoadLocalUiSettingsAsync()
    {
        try
        {
            if (!File.Exists(_uiSettingsFilePath))
            {
                _localUiSettings = new LocalUiSettings();
                ApplySettingsToUi();
                return;
            }

            var json = await File.ReadAllTextAsync(_uiSettingsFilePath);
            _localUiSettings = JsonSerializer.Deserialize<LocalUiSettings>(json) ?? new LocalUiSettings();
            if (_localUiSettings.DefaultRundenanzahlProStint <= 0)
            {
                _localUiSettings.DefaultRundenanzahlProStint = 10;
            }

            _localUiSettings.TrainingRundenanzahlOverrides ??= new Dictionary<int, int>();

            ApplySettingsToUi();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Lokale UI-Einstellungen konnten nicht geladen werden.");
            _localUiSettings = new LocalUiSettings();
            ApplySettingsToUi();
        }
    }

    private void ApplySettingsToUi()
    {
        if (DefaultStintRoundsTextBox is null)
        {
            return;
        }

        DefaultStintRoundsTextBox.Text = _localUiSettings.DefaultRundenanzahlProStint.ToString();
        SettingsFeedbackTextBlock.Text = string.Empty;

        ApplyTrainingRoundsToUi();
    }

    private int GetRoundsTargetForTraining(int trainingId)
    {
        if (_localUiSettings.TrainingRundenanzahlOverrides.TryGetValue(trainingId, out var rounds) && rounds > 0)
        {
            return rounds;
        }

        return _localUiSettings.DefaultRundenanzahlProStint;
    }

    private void ApplyTrainingRoundsToUi()
    {
        if (TrainingRoundsTextBox is null || TrainingLapCounterTextBlock is null)
        {
            return;
        }

        if (_selectedTrainingDetailId is null)
        {
            TrainingRoundsTextBox.Text = _localUiSettings.DefaultRundenanzahlProStint.ToString();
            if (TrainingRoundsFeedbackTextBlock is not null)
            {
                TrainingRoundsFeedbackTextBlock.Text = string.Empty;
            }

            UpdateTrainingLapProgressDisplay();
            UpdateTrainingStopwatchButtonsState();

            return;
        }

        var rounds = GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        TrainingRoundsTextBox.Text = rounds.ToString();
        if (TrainingRoundsFeedbackTextBlock is not null)
        {
            TrainingRoundsFeedbackTextBlock.Text = string.Empty;
        }

        UpdateTrainingLapProgressDisplay();
        UpdateTrainingStopwatchButtonsState();
    }

    private async Task SaveLocalUiSettingsAsync()
    {
        var settingsDirectory = Path.GetDirectoryName(_uiSettingsFilePath);
        if (!string.IsNullOrWhiteSpace(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        var json = JsonSerializer.Serialize(_localUiSettings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_uiSettingsFilePath, json);
    }

    private (int TrainingId, int FahrerId)? GetActiveTrainingDriverContext()
    {
        if (_selectedTrainingDetailId is null)
        {
            return null;
        }

        var activeDriver = TrainingStarterListItems.FirstOrDefault(x => x.IsAktiv);
        if (activeDriver is null)
        {
            return null;
        }

        return (_selectedTrainingDetailId.Value, activeDriver.FahrerId);
    }

    private TrainingStintState GetOrCreateTrainingStintState((int TrainingId, int FahrerId) context)
    {
        if (_trainingStintsByDriver.TryGetValue(context, out var existingState))
        {
            return existingState;
        }

        var state = new TrainingStintState();
        _trainingStintsByDriver[context] = state;
        return state;
    }

    private static string FormatTrainingTime(TimeSpan elapsed)
    {
        return $"{(int)elapsed.TotalSeconds:00}.{elapsed.Milliseconds:000}";
    }

    private double CalculateLapPenaltySeconds(TrainingLapTimeListItem lap)
    {
        var raw = (lap.Tore * _selectedTrainingTorfehlerPenaltySeconds) + (lap.Pylonen * _selectedTrainingPylonenfehlerPenaltySeconds);
        return Math.Round(Math.Max(0d, raw), 3, MidpointRounding.AwayFromZero);
    }

    private void RecalculateLapPenaltiesForCurrentContext()
    {
        if (_trainingStopwatchContext is null ||
            !_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state))
        {
            return;
        }

        foreach (var lap in state.LapRecords)
        {
            lap.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lap);
        }
    }

    private void UpdateTrainingLapProgressDisplay()
    {
        if (TrainingLapCounterTextBlock is null)
        {
            return;
        }

        if (_selectedTrainingDetailId is null || _trainingStopwatchContext is null)
        {
            TrainingLapCounterTextBlock.Text = "Runde: -/-";
            return;
        }

        var roundsTarget = GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        var currentLap = roundsTarget > 0 && state.LapRecords.Count >= roundsTarget
            ? roundsTarget
            : state.LapRecords.Count + 1;

        TrainingLapCounterTextBlock.Text = roundsTarget > 0
            ? $"Runde: {currentLap}/{roundsTarget}"
            : $"Runde: {currentLap}/-";
    }

    private void UpdateTrainingLapSummaryDisplay()
    {
        if (TrainingTotalTimeTextBlock is null || TrainingAverageTimeTextBlock is null)
        {
            return;
        }

        if (_trainingStopwatchContext is null ||
            !_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state) ||
            state.LapRecords.Count == 0)
        {
            TrainingTotalTimeTextBlock.Text = "-";
            TrainingAverageTimeTextBlock.Text = "-";
            return;
        }

        var validLaps = state.LapRecords.Where(x => !x.Ungueltig).ToList();
        if (validLaps.Count == 0)
        {
            TrainingTotalTimeTextBlock.Text = "-";
            TrainingAverageTimeTextBlock.Text = "-";
            return;
        }

        var totalSeconds = validLaps.Sum(x => x.Rundenzeit.TotalSeconds + x.ZeitstrafeSekunden);
        var totalTime = TimeSpan.FromSeconds(totalSeconds);
        var avgTime = TimeSpan.FromSeconds(totalSeconds / validLaps.Count);

        TrainingTotalTimeTextBlock.Text = FormatTrainingTime(totalTime);
        TrainingAverageTimeTextBlock.Text = FormatTrainingTime(avgTime);
    }

    private void RefreshTrainingLapTimesTable()
    {
        TrainingLapTimeItems.Clear();
        if (_trainingStopwatchContext is null)
        {
            UpdateTrainingLapSummaryDisplay();
            return;
        }

        if (!_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state))
        {
            UpdateTrainingLapSummaryDisplay();
            return;
        }

        foreach (var lap in state.LapRecords)
        {
            TrainingLapTimeItems.Add(lap);
        }

        UpdateTrainingLapSummaryDisplay();
    }

    private void SyncTrainingStopwatchContextWithActiveDriver(bool resetIfContextChanges)
    {
        var newContext = GetActiveTrainingDriverContext();
        if (_trainingStopwatchContext == newContext)
        {
            RefreshTrainingLapTimesTable();
            UpdateTrainingStopwatchDisplay();
            UpdateTrainingStopwatchButtonsState();
            return;
        }

        if (_trainingStopwatchContext is not null &&
            _trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var previousState) &&
            previousState.Stopwatch.IsRunning)
        {
            previousState.Stopwatch.Stop();
        }

        _trainingStopwatchContext = newContext;

        if (_trainingStopwatchContext is null)
        {
            if (_trainingStopwatchTimer.IsEnabled)
            {
                _trainingStopwatchTimer.Stop();
            }

            TrainingLapTimeItems.Clear();
            TrainingStopwatchTextBlock.Text = "00.000";
            UpdateTrainingLapSummaryDisplay();
            UpdateTrainingLapProgressDisplay();
            UpdateTrainingStopwatchButtonsState();
            return;
        }

        var currentState = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        if (resetIfContextChanges)
        {
            currentState.Stopwatch.Reset();
            currentState.LapRecords.Clear();
            currentState.LastLapCheckpoint = TimeSpan.Zero;
        }

        RefreshTrainingLapTimesTable();
        UpdateTrainingStopwatchDisplay();

        if (currentState.Stopwatch.IsRunning)
        {
            if (!_trainingStopwatchTimer.IsEnabled)
            {
                _trainingStopwatchTimer.Start();
            }
        }
        else if (_trainingStopwatchTimer.IsEnabled)
        {
            _trainingStopwatchTimer.Stop();
        }

        UpdateTrainingStopwatchButtonsState();
    }

    private void ResetTrainingStopwatchView()
    {
        if (_trainingStopwatchContext is not null &&
            _trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var currentState) &&
            currentState.Stopwatch.IsRunning)
        {
            currentState.Stopwatch.Stop();
        }

        _trainingStopwatchContext = null;
        if (_trainingStopwatchTimer.IsEnabled)
        {
            _trainingStopwatchTimer.Stop();
        }

        TrainingLapTimeItems.Clear();
        TrainingStopwatchTextBlock.Text = "00.000";
        UpdateTrainingLapSummaryDisplay();
        UpdateTrainingLapProgressDisplay();
        UpdateTrainingStopwatchButtonsState();
    }

    private void UpdateConnectionStatus()
    {
        var localStatus = _databaseRuntimeInfo.LocalSqliteConnected ? "Verbunden" : "Nicht verbunden";
        var remoteStatus = _databaseRuntimeInfo.RemoteMySqlConnected ? "Verbunden" : "Nicht verbunden";

        FooterDbStatusTextBlock.Text = $"SQLite: {localStatus} | MySQL: {remoteStatus}";

        if (!_databaseRuntimeInfo.LocalSqliteConnected && !string.IsNullOrWhiteSpace(_databaseRuntimeInfo.LocalSqliteError))
        {
            FooterDbStatusTextBlock.Text += $" | SQLite-Fehler: {_databaseRuntimeInfo.LocalSqliteError}";
            FooterDbStatusTextBlock.ToolTip = _databaseRuntimeInfo.LocalSqliteError;
        }

        if (!_databaseRuntimeInfo.RemoteMySqlConnected && !string.IsNullOrWhiteSpace(_databaseRuntimeInfo.RemoteMySqlError))
        {
            FooterDbStatusTextBlock.ToolTip = $"SQLite: {_databaseRuntimeInfo.LocalSqliteError}\nMySQL: {_databaseRuntimeInfo.RemoteMySqlError}";
        }
    }

    private async Task RefreshSyncStatusAsync()
    {
        try
        {
            var status = await _dataSyncService.GetSyncStatusAsync();
            _syncNeeded = status.IsSyncNeeded;

            FooterSyncStateTextBlock.Text = status.Message;
            FooterSyncStateTextBlock.ToolTip = status.Message;
            UpdateSyncButtonVisualState();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Pruefen des Synchronisierungsstatus.");
            _syncNeeded = false;
            FooterSyncStateTextBlock.Text = "Sync-Status konnte nicht geladen werden.";
            FooterSyncStateTextBlock.ToolTip = ex.Message;
            UpdateSyncButtonVisualState();
        }
    }

    private void UpdateSyncButtonVisualState()
    {
        if (_syncInProgress)
        {
            FooterSyncButton.IsEnabled = false;
            FooterSyncButton.Content = "Synchronisiert...";
            FooterSyncButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            FooterSyncButton.Foreground = Brushes.White;
            return;
        }

        FooterSyncButton.IsEnabled = true;
        FooterSyncButton.Content = _syncNeeded ? "Synchronisieren" : "Synchronisiert";

        if (_syncNeeded)
        {
            FooterSyncButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            FooterSyncButton.Foreground = Brushes.Black;
        }
        else
        {
            FooterSyncButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
            FooterSyncButton.Foreground = Brushes.White;
        }
    }

    private void NavigateTo(string page)
    {
        _selectedTrainingDetailId = null;
        CloseTrainingDriverSelectionDialog();
        TrainingStarterListItems.Clear();
        TrainingFastestLapItems.Clear();
        ResetTrainingStopwatchView();
        StartseitePage.Visibility = Visibility.Collapsed;
        TrainingsPage.Visibility = Visibility.Collapsed;
        TrainingDetailPage.Visibility = Visibility.Collapsed;
        MeisterschaftenPage.Visibility = Visibility.Collapsed;
        VereinePage.Visibility = Visibility.Collapsed;
        DisziplinPage.Visibility = Visibility.Collapsed;
        FahrerPage.Visibility = Visibility.Collapsed;
        KartsPage.Visibility = Visibility.Collapsed;
        WetterPage.Visibility = Visibility.Collapsed;
        StatistikenPage.Visibility = Visibility.Collapsed;
        EinstellungenPage.Visibility = Visibility.Collapsed;

        switch (page)
        {
            case "Startseite":
                StartseitePage.Visibility = Visibility.Visible;
                break;
            case "Trainings":
                TrainingsPage.Visibility = Visibility.Visible;
                _ = LoadTrainingsAsync();
                break;
            case "Meisterschaften":
                MeisterschaftenPage.Visibility = Visibility.Visible;
                break;
            case "Vereine":
                VereinePage.Visibility = Visibility.Visible;
                _ = LoadVereineAsync();
                break;
            case "Disziplin":
                DisziplinPage.Visibility = Visibility.Visible;
                _ = LoadDisziplinenAsync();
                break;
            case "Fahrer":
                FahrerPage.Visibility = Visibility.Visible;
                _ = LoadFahrerAsync();
                break;
            case "Karts":
                KartsPage.Visibility = Visibility.Visible;
                _ = LoadKartsAsync();
                break;
            case "Wetter":
                WetterPage.Visibility = Visibility.Visible;
                _ = LoadWetterAsync();
                break;
            case "Statistiken":
                StatistikenPage.Visibility = Visibility.Visible;
                break;
            case "Einstellungen":
                EinstellungenPage.Visibility = Visibility.Visible;
                ApplySettingsToUi();
                break;
            default:
                if (TryParseTrainingDetailTag(page, out var trainingId))
                {
                    _selectedTrainingDetailId = trainingId;
                    TrainingDetailPage.Visibility = Visibility.Visible;
                    _ = LoadTrainingDetailAsync(trainingId);
                    break;
                }

                StartseitePage.Visibility = Visibility.Visible;
                page = "Startseite";
                break;
        }

        ApplyTrainingRoundsToUi();
        ApplyMenuSelection(page);
    }

    private static bool TryParseTrainingDetailTag(string value, out int trainingId)
    {
        trainingId = 0;
        if (!value.StartsWith(TrainingDetailTagPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(value[TrainingDetailTagPrefix.Length..], out trainingId);
    }

    private async Task LoadTrainingDetailAsync(int trainingId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings
                .AsNoTracking()
                .Include(x => x.Verein)
                .Include(x => x.Disziplin)
                .Include(x => x.Wetter)
                .FirstOrDefaultAsync(x => x.Id == trainingId);

            if (training is null)
            {
                _selectedTrainingTorfehlerPenaltySeconds = 0d;
                _selectedTrainingPylonenfehlerPenaltySeconds = 0d;
                TrainingStarterListItems.Clear();
                TrainingFastestLapItems.Clear();
                UpdateTrainingDriverButtonsState();
                TrainingDetailTitleTextBlock.Text = "Training nicht gefunden";
                TrainingDetailSubtitleTextBlock.Text = "Das ausgewaehlte Training ist nicht mehr verfuegbar.";
                TrainingDetailStatusTextBlock.Text = "Status: -";
                TrainingDetailZeitpunktTextBlock.Text = "Datum: -";
                TrainingDetailVereinTextBlock.Text = "Verein: -";
                TrainingDetailDisziplinTextBlock.Text = "Disziplin: -";
                TrainingDetailWetterTextBlock.Text = "Wetter: -";
                TrainingDetailBeschreibungTextBlock.Text = "Beschreibung: -";
                ApplyTrainingRoundsToUi();
                return;
            }

            TrainingDetailTitleTextBlock.Text = training.Name;
            TrainingDetailSubtitleTextBlock.Text = $"Training #{training.Id}";
            TrainingDetailStatusTextBlock.Text = $"Status: {(training.TrainingAbgeschlossen ? "Abgeschlossen" : "Offen")}";
            TrainingDetailZeitpunktTextBlock.Text = $"Datum: {training.Zeitpunkt:dd.MM.yyyy}";
            TrainingDetailVereinTextBlock.Text = $"Verein: {training.Verein.Vereinsname}";
            TrainingDetailDisziplinTextBlock.Text = $"Disziplin: {training.Disziplin.Name}";
            TrainingDetailWetterTextBlock.Text = $"Wetter: {training.Wetter.Bezeichnung}";
            TrainingDetailBeschreibungTextBlock.Text = $"Beschreibung: {training.Beschreibung}";
            _selectedTrainingTorfehlerPenaltySeconds = training.Disziplin.ZeitstrafeTorfehler;
            _selectedTrainingPylonenfehlerPenaltySeconds = training.Disziplin.ZeitstrafePylonenfehler;
            RecalculateLapPenaltiesForCurrentContext();
            UpdateTrainingLapSummaryDisplay();
            await LoadTrainingStarterListAsync(training.Id);
            await LoadTrainingFastestLapsAsync(training.Id);
            ApplyTrainingRoundsToUi();
        }
        catch (Exception ex)
        {
            _selectedTrainingTorfehlerPenaltySeconds = 0d;
            _selectedTrainingPylonenfehlerPenaltySeconds = 0d;
            Logger.Error(ex, "Fehler beim Laden der Trainingsdetailansicht.");
            TrainingStarterListItems.Clear();
            TrainingFastestLapItems.Clear();
            UpdateTrainingDriverButtonsState();
            TrainingDetailTitleTextBlock.Text = "Trainingsdetail nicht verfuegbar";
            TrainingDetailSubtitleTextBlock.Text = "Fehler beim Laden der Daten.";
            TrainingDetailStatusTextBlock.Text = "Status: -";
            TrainingDetailZeitpunktTextBlock.Text = "Datum: -";
            TrainingDetailVereinTextBlock.Text = "Verein: -";
            TrainingDetailDisziplinTextBlock.Text = "Disziplin: -";
            TrainingDetailWetterTextBlock.Text = "Wetter: -";
            TrainingDetailBeschreibungTextBlock.Text = "Beschreibung: -";
            ApplyTrainingRoundsToUi();
        }
    }

    private async Task LoadTrainingStarterListAsync(int trainingId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var starter = await dbContext.FahrerImTrainings
                .AsNoTracking()
                .Where(x => x.TrainingId == trainingId)
                .Include(x => x.Fahrer)
                .ThenInclude(x => x.Verein)
                .OrderBy(x => x.Reihenfolge)
                .ThenBy(x => x.Fahrer.Vorname)
                .ThenBy(x => x.Fahrer.Nachname)
                .Select(x => new TrainingStarterListItem
                {
                    FahrerId = x.FahrerId,
                    Reihenfolge = x.Reihenfolge,
                    Vorname = x.Fahrer.Vorname,
                    Nachname = x.Fahrer.Nachname ?? string.Empty,
                    VereinName = x.Fahrer.Verein.Vereinsname
                })
                .ToListAsync();

            if (starter.Count == 0)
            {
                _trainingActiveDriverByTrainingId.Remove(trainingId);
            }
            else
            {
                foreach (var item in starter)
                {
                    if (_trainingDriverEnabledByDriver.TryGetValue((trainingId, item.FahrerId), out var enabled))
                    {
                        item.FahrerFaehrt = enabled;
                    }
                    else
                    {
                        item.FahrerFaehrt = true;
                    }

                    if (_trainingKartSelectionByDriver.TryGetValue((trainingId, item.FahrerId), out var selectedKartId))
                    {
                        item.KartId = selectedKartId;
                    }
                }

                var enabledStarter = starter.Where(x => x.FahrerFaehrt).ToList();
                if (!_trainingActiveDriverByTrainingId.TryGetValue(trainingId, out var activeFahrerId) ||
                    enabledStarter.All(x => x.FahrerId != activeFahrerId))
                {
                    if (enabledStarter.Count > 0)
                    {
                        activeFahrerId = enabledStarter[0].FahrerId;
                        _trainingActiveDriverByTrainingId[trainingId] = activeFahrerId;
                    }
                    else
                    {
                        _trainingActiveDriverByTrainingId.Remove(trainingId);
                    }
                }

                for (var i = 0; i < starter.Count; i++)
                {
                    var item = starter[i];
                    item.Nummer = i + 1;
                    item.IsAktiv = _trainingActiveDriverByTrainingId.TryGetValue(trainingId, out var currentActiveId) && item.FahrerId == currentActiveId;
                }
            }

            TrainingStarterListItems.Clear();
            foreach (var item in starter)
            {
                TrainingStarterListItems.Add(item);
            }

            UpdateTrainingDriverButtonsState();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Starterliste.");
            TrainingStarterListItems.Clear();
            UpdateTrainingDriverButtonsState();
        }
    }

    private async Task LoadTrainingFastestLapsAsync(int trainingId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var penalties = await dbContext.Trainings
                .AsNoTracking()
                .Where(x => x.Id == trainingId)
                .Select(x => new
                {
                    Torfehler = x.Disziplin.ZeitstrafeTorfehler,
                    Pylonenfehler = x.Disziplin.ZeitstrafePylonenfehler
                })
                .FirstOrDefaultAsync();

            var torfehlerPenalty = penalties?.Torfehler ?? 0d;
            var pylonenfehlerPenalty = penalties?.Pylonenfehler ?? 0d;

            var lapRows = await dbContext.Trunden
                .AsNoTracking()
                .Where(x => x.Tstint != null && x.Tstint.TrainingId == trainingId)
                .Select(x => new
                {
                    FahrerId = x.Tstint!.FahrerId,
                    x.Tstint.Fahrer.Vorname,
                    Nachname = x.Tstint.Fahrer.Nachname ?? string.Empty,
                    KartName = x.Tstint.Kart != null ? x.Tstint.Kart.Name : null,
                    Zeitpunkt = x.Tstint.Datum,
                    Runde = x.Runde ?? 0,
                    Rundenzeit = x.Rundenzeit,
                    Pylonen = x.Pf ?? 0,
                    Tore = x.Tf ?? 0,
                    x.Ungueltig
                })
                .ToListAsync();

            var perDriver = lapRows
                .GroupBy(x => x.FahrerId)
                .Select(group =>
                {
                    var validLaps = group
                        .Where(x => !x.Ungueltig && x.Rundenzeit.HasValue && x.Rundenzeit.Value > 0)
                        .Select(x => new
                        {
                            Row = x,
                            PenaltySeconds = (x.Tore * torfehlerPenalty) + (x.Pylonen * pylonenfehlerPenalty),
                        })
                        .Select(x => new
                        {
                            x.Row,
                            PenaltySeconds = Math.Max(0d, x.PenaltySeconds),
                            EffectiveSeconds = x.Row.Rundenzeit!.Value + Math.Max(0d, x.PenaltySeconds)
                        })
                        .OrderBy(x => x.EffectiveSeconds)
                        .ThenBy(x => x.Row.Zeitpunkt)
                        .ToList();

                    if (validLaps.Count == 0)
                    {
                        return null;
                    }

                    var fastest = validLaps[0];
                    var fahrerName = string.IsNullOrWhiteSpace(fastest.Row.Nachname)
                        ? fastest.Row.Vorname
                        : $"{fastest.Row.Vorname} {fastest.Row.Nachname}";

                    return new
                    {
                        fastest.EffectiveSeconds,
                        Fahrer = fahrerName,
                        Kart = string.IsNullOrWhiteSpace(fastest.Row.KartName) ? "-" : fastest.Row.KartName!,
                        RundenzeitText = FormatTrainingTime(TimeSpan.FromSeconds(fastest.EffectiveSeconds)),
                        StrafenText = $"{fastest.Row.Pylonen}P {fastest.Row.Tore}T (+{FormatSecondsValue(fastest.PenaltySeconds)}s)",
                        ZeitpunktText = fastest.Row.Zeitpunkt.ToString("dd.MM.yyyy HH:mm"),
                        Runden = group.Count()
                    };
                })
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderBy(x => x.EffectiveSeconds)
                .ThenBy(x => x.Fahrer)
                .ToList();

            TrainingFastestLapItems.Clear();
            if (perDriver.Count == 0)
            {
                return;
            }

            var bestSeconds = perDriver[0].EffectiveSeconds;
            for (var i = 0; i < perDriver.Count; i++)
            {
                var row = perDriver[i];
                var diff = row.EffectiveSeconds - bestSeconds;

                TrainingFastestLapItems.Add(new TrainingFastestLapListItem
                {
                    Position = i + 1,
                    Fahrer = row.Fahrer,
                    Kart = row.Kart,
                    RundenzeitText = row.RundenzeitText,
                    DiffText = i == 0 ? "-" : $"+{FormatTrainingTime(TimeSpan.FromSeconds(diff))}",
                    StrafenText = row.StrafenText,
                    ZeitpunktText = row.ZeitpunktText,
                    Runden = row.Runden
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der schnellsten Runden pro Fahrer.");
            TrainingFastestLapItems.Clear();
        }
    }

    private void ApplyMenuSelection(string selectedTag)
    {
        _selectedMenuTag = selectedTag;

        foreach (var button in GetMenuButtons())
        {
            if (button.Tag is not string tag)
            {
                continue;
            }

            if (tag == selectedTag)
            {
                button.Background = ActiveMenuBackgroundBrush;
                button.Foreground = ActiveMenuForegroundBrush;
            }
            else
            {
                button.Background = DefaultMenuBackgroundBrush;
                button.Foreground = DefaultMenuForegroundBrush;
            }
        }
    }

    private void ConfigureMenuButtons()
    {
        foreach (var button in GetMenuButtons())
        {
            ConfigureMenuButton(button);
        }
    }

    private void ConfigureMenuButton(Button button)
    {
        if (button.Tag is not string)
        {
            return;
        }

        button.MouseEnter -= MenuButton_OnMouseEnter;
        button.MouseLeave -= MenuButton_OnMouseLeave;
        button.MouseEnter += MenuButton_OnMouseEnter;
        button.MouseLeave += MenuButton_OnMouseLeave;
        button.Background = DefaultMenuBackgroundBrush;
        button.Foreground = DefaultMenuForegroundBrush;
    }

    private IEnumerable<Button> GetMenuButtons()
    {
        return MenuPanel.Children.OfType<Button>();
    }

    private void RefreshOpenTrainingMenuButtons()
    {
        foreach (var button in _dynamicTrainingMenuButtons)
        {
            MenuPanel.Children.Remove(button);
        }

        _dynamicTrainingMenuButtons.Clear();

        var trainingsButtonIndex = MenuPanel.Children.IndexOf(MenuTrainings);
        if (trainingsButtonIndex < 0)
        {
            return;
        }

        var insertIndex = trainingsButtonIndex + 1;
        var openTrainings = TrainingItems
            .Where(x => !x.TrainingAbgeschlossen)
            .OrderBy(x => x.Zeitpunkt)
            .ThenBy(x => x.Name)
            .ToList();

        foreach (var training in openTrainings)
        {
            var button = new Button
            {
                Style = (Style)FindResource("MenuButtonStyle"),
                Padding = new Thickness(28, 8, 10, 8),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = $"- {training.Name}",
                Tag = $"{TrainingDetailTagPrefix}{training.Id}"
            };

            button.Click += MenuButton_OnClick;
            ConfigureMenuButton(button);
            MenuPanel.Children.Insert(insertIndex, button);
            _dynamicTrainingMenuButtons.Add(button);
            insertIndex++;
        }

        if (_selectedMenuTag.StartsWith(TrainingDetailTagPrefix, StringComparison.Ordinal) &&
            _dynamicTrainingMenuButtons.All(x => !string.Equals(x.Tag as string, _selectedMenuTag, StringComparison.Ordinal)))
        {
            _selectedMenuTag = "Trainings";
            NavigateTo("Trainings");
            return;
        }

        ApplyMenuSelection(_selectedMenuTag);
    }

    private void MenuButton_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string)
        {
            return;
        }

        var baseColor = ResolveMenuBackgroundColor(button);
        var hoverColor = DarkenColor(baseColor, HoverDarkenFactor);

        button.Background = new SolidColorBrush(hoverColor);
        button.Foreground = GetReadableForeground(hoverColor);
    }

    private void MenuButton_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
        {
            return;
        }

        if (tag == _selectedMenuTag)
        {
            button.Background = ActiveMenuBackgroundBrush;
            button.Foreground = ActiveMenuForegroundBrush;
            return;
        }

        button.Background = DefaultMenuBackgroundBrush;
        button.Foreground = DefaultMenuForegroundBrush;
    }

    private static Color ResolveMenuBackgroundColor(Button button)
    {
        if (button.Background is SolidColorBrush solid && solid.Color.A > 0)
        {
            return solid.Color;
        }

        return MenuFallbackBackgroundColor;
    }

    private static Color DarkenColor(Color color, double factor)
    {
        var normalizedFactor = Math.Clamp(factor, 0, 1);
        var multiplier = 1 - normalizedFactor;

        return Color.FromArgb(
            color.A,
            (byte)(color.R * multiplier),
            (byte)(color.G * multiplier),
            (byte)(color.B * multiplier));
    }

    private static Brush GetReadableForeground(Color backgroundColor)
    {
        var brightness = ((backgroundColor.R * 299) + (backgroundColor.G * 587) + (backgroundColor.B * 114)) / 1000.0;
        return brightness < 140 ? Brushes.White : Brushes.Black;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(dependencyObject);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(dependencyObject, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private void MenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void BackToTrainingsPage_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("Trainings");
    }

    private async void SaveSettings_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DefaultStintRoundsTextBox.Text.Trim(), out var defaultRounds) || defaultRounds <= 0)
        {
            SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            SettingsFeedbackTextBlock.Text = "Bitte eine gueltige, positive Rundenanzahl eingeben.";
            return;
        }

        try
        {
            _localUiSettings.DefaultRundenanzahlProStint = defaultRounds;
            await SaveLocalUiSettingsAsync();
            ApplyTrainingRoundsToUi();

            SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            SettingsFeedbackTextBlock.Text = "Einstellungen wurden lokal gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Lokale UI-Einstellungen konnten nicht gespeichert werden.");
            SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            SettingsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }

    private async void SaveTrainingRounds_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null)
        {
            TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingRoundsFeedbackTextBlock.Text = "Bitte zuerst ein Training auswaehlen.";
            return;
        }

        if (!int.TryParse(TrainingRoundsTextBox.Text.Trim(), out var trainingRounds) || trainingRounds <= 0)
        {
            TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingRoundsFeedbackTextBlock.Text = "Bitte eine gueltige, positive Rundenanzahl eingeben.";
            return;
        }

        try
        {
            _localUiSettings.TrainingRundenanzahlOverrides[_selectedTrainingDetailId.Value] = trainingRounds;
            await SaveLocalUiSettingsAsync();
            ApplyTrainingRoundsToUi();

            TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            TrainingRoundsFeedbackTextBlock.Text = "Rundenanzahl fuer das Training gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Trainingsspezifische Rundenanzahl konnte nicht gespeichert werden.");
            TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingRoundsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }

    private void SkipTrainingDriver_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null || TrainingStarterListItems.Count == 0)
        {
            return;
        }

        var ordered = TrainingStarterListItems
            .Where(x => x.FahrerFaehrt)
            .OrderBy(x => x.Reihenfolge)
            .ThenBy(x => x.FahrerId)
            .ToList();

        if (ordered.Count == 0)
        {
            return;
        }

        var currentIndex = ordered.FindIndex(x => x.IsAktiv);
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % ordered.Count;
        var nextActive = ordered[nextIndex].FahrerId;

        _trainingActiveDriverByTrainingId[_selectedTrainingDetailId.Value] = nextActive;

        foreach (var item in TrainingStarterListItems)
        {
            item.IsAktiv = item.FahrerId == nextActive;
        }

        TrainingStarterDataGrid.Items.Refresh();
        UpdateTrainingDriverButtonsState();
    }

    private async void FinishTraining_OnClick(object sender, RoutedEventArgs e)
    {
        if (_finishTrainingInProgress)
        {
            return;
        }

        if (_selectedTrainingDetailId is null)
        {
            return;
        }

        _finishTrainingInProgress = true;
        FinishTrainingButton.IsEnabled = false;

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == _selectedTrainingDetailId.Value);
            if (training is null)
            {
                return;
            }

            if (!training.TrainingAbgeschlossen)
            {
                training.TrainingAbgeschlossen = true;
                await dbContext.SaveChangesAsync();
            }

            await RefreshSyncStatusAsync();
            NavigateTo("Trainings");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Training konnte nicht als abgeschlossen markiert werden.");
            MessageBox.Show("Training konnte nicht beendet werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _finishTrainingInProgress = false;
            if (FinishTrainingButton is not null)
            {
                FinishTrainingButton.IsEnabled = true;
            }
        }
    }

    private bool IsCurrentStintFinished()
    {
        if (_selectedTrainingDetailId is null)
        {
            return false;
        }

        var activeDriver = TrainingStarterListItems.FirstOrDefault(x => x.IsAktiv && x.FahrerFaehrt);
        if (activeDriver is null)
        {
            return false;
        }

        var context = (_selectedTrainingDetailId.Value, activeDriver.FahrerId);
        if (!_trainingStintsByDriver.TryGetValue(context, out var state) || state.Stopwatch.IsRunning)
        {
            return false;
        }

        var roundsTarget = GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        return roundsTarget > 0 && state.LapRecords.Count >= roundsTarget;
    }

    private async void NextTrainingDriver_OnClick(object sender, RoutedEventArgs e)
    {
        if (_nextDriverSwitchInProgress)
        {
            return;
        }

        _nextDriverSwitchInProgress = true;
        UpdateTrainingDriverButtonsState();

        if (_selectedTrainingDetailId is null)
        {
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
            return;
        }

        TrainingLapTimesDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        TrainingLapTimesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

        var trainingId = _selectedTrainingDetailId.Value;
        var ordered = TrainingStarterListItems
            .Where(x => x.FahrerFaehrt)
            .OrderBy(x => x.Reihenfolge)
            .ThenBy(x => x.FahrerId)
            .ToList();

        if (ordered.Count == 0)
        {
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
            return;
        }

        var currentIndex = ordered.FindIndex(x => x.IsAktiv);
        if (currentIndex < 0)
        {
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
            return;
        }

        var currentDriverId = ordered[currentIndex].FahrerId;
        var currentKartId = ordered[currentIndex].KartId;
        var currentContext = (trainingId, currentDriverId);

        if (!_trainingStintsByDriver.TryGetValue(currentContext, out var currentState) || currentState.Stopwatch.IsRunning)
        {
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
            return;
        }

        var roundsTarget = GetRoundsTargetForTraining(trainingId);
        if (roundsTarget <= 0 || currentState.LapRecords.Count < roundsTarget)
        {
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var stint = new Tstint
            {
                TrainingId = trainingId,
                FahrerId = currentDriverId,
                KartId = currentKartId,
                Datum = DateTime.Now
            };
            dbContext.Tstints.Add(stint);

            foreach (var lap in currentState.LapRecords.OrderBy(x => x.Nummer))
            {
                dbContext.Trunden.Add(new Trunde
                {
                    Tstint = stint,
                    Runde = lap.Nummer,
                    Rundenzeit = lap.Rundenzeit.TotalSeconds,
                    Pf = lap.Pylonen,
                    Tf = lap.Tore,
                    Ungueltig = lap.Ungueltig
                });
            }

            await dbContext.SaveChangesAsync();
            await LoadTrainingFastestLapsAsync(trainingId);
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Stint konnte nicht gespeichert werden.");
            MessageBox.Show("Stint konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
            return;
        }

        var nextIndex = (currentIndex + 1) % ordered.Count;
        var nextDriverId = ordered[nextIndex].FahrerId;

        _trainingStintsByDriver.Remove(currentContext);
        _trainingStintsByDriver.Remove((trainingId, nextDriverId));

        _trainingActiveDriverByTrainingId[trainingId] = nextDriverId;
        foreach (var item in TrainingStarterListItems)
        {
            item.IsAktiv = item.FahrerId == nextDriverId;
        }

        TrainingStarterDataGrid.Items.Refresh();
        _nextDriverSwitchInProgress = false;
        UpdateTrainingDriverButtonsState();
    }

    private void TrainingStarterFaehrtCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null || sender is not CheckBox checkBox || checkBox.DataContext is not TrainingStarterListItem selected)
        {
            return;
        }

        selected.FahrerFaehrt = checkBox.IsChecked == true;
        _trainingDriverEnabledByDriver[(_selectedTrainingDetailId.Value, selected.FahrerId)] = selected.FahrerFaehrt;

        var enabledOrdered = TrainingStarterListItems
            .Where(x => x.FahrerFaehrt)
            .OrderBy(x => x.Reihenfolge)
            .ThenBy(x => x.FahrerId)
            .ToList();

        if (enabledOrdered.Count == 0)
        {
            _trainingActiveDriverByTrainingId.Remove(_selectedTrainingDetailId.Value);
            foreach (var item in TrainingStarterListItems)
            {
                item.IsAktiv = false;
            }

            TrainingStarterDataGrid.Items.Refresh();
            UpdateTrainingDriverButtonsState();
            return;
        }

        if (!_trainingActiveDriverByTrainingId.TryGetValue(_selectedTrainingDetailId.Value, out var activeId) ||
            enabledOrdered.All(x => x.FahrerId != activeId))
        {
            activeId = enabledOrdered[0].FahrerId;
            _trainingActiveDriverByTrainingId[_selectedTrainingDetailId.Value] = activeId;
        }

        foreach (var item in TrainingStarterListItems)
        {
            item.IsAktiv = item.FahrerId == activeId;
        }

        TrainingStarterDataGrid.Items.Refresh();
        UpdateTrainingDriverButtonsState();
    }

    private void TrainingStarterKartComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedTrainingDetailId is null || sender is not ComboBox comboBox || comboBox.DataContext is not TrainingStarterListItem item)
        {
            return;
        }

        item.KartId = comboBox.SelectedValue as int?;
        _trainingKartSelectionByDriver[(_selectedTrainingDetailId.Value, item.FahrerId)] = item.KartId;
    }

    private void UpdateTrainingDriverButtonsState()
    {
        var hasStarter = TrainingStarterListItems.Count > 0;
        var hasActive = TrainingStarterListItems.Any(x => x.IsAktiv);
        var hasEnabled = TrainingStarterListItems.Any(x => x.FahrerFaehrt);
        var canSwitchToNextDriver = !_nextDriverSwitchInProgress && hasStarter && hasActive && IsCurrentStintFinished();

        NextDriverButton.IsEnabled = canSwitchToNextDriver;
        NextDriverButton.Background = canSwitchToNextDriver
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F84DE"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

        SkipDriverButton.IsEnabled = hasStarter && hasEnabled;
        SkipDriverButton.Background = hasStarter && hasEnabled
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

        UpdateActiveDriverDisplay();
    }

    private void UpdateActiveDriverDisplay()
    {
        var activeDriver = TrainingStarterListItems.FirstOrDefault(x => x.IsAktiv);
        if (activeDriver is null)
        {
            TrainingActiveDriverTextBlock.Text = "-";
            SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
            return;
        }

        TrainingActiveDriverTextBlock.Text = string.IsNullOrWhiteSpace(activeDriver.Nachname)
            ? activeDriver.Vorname
            : $"{activeDriver.Vorname} {activeDriver.Nachname}";
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
    }

    private void TrainingStopwatchTimer_OnTick(object? sender, EventArgs e)
    {
        UpdateTrainingStopwatchDisplay();
    }

    private void TrainingStopwatchStart_OnClick(object sender, RoutedEventArgs e)
    {
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
        if (_trainingStopwatchContext is null)
        {
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        state.Stopwatch.Start();
        if (!_trainingStopwatchTimer.IsEnabled)
        {
            _trainingStopwatchTimer.Start();
        }

        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    private void TrainingStopwatchStop_OnClick(object sender, RoutedEventArgs e)
    {
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
        if (_trainingStopwatchContext is null)
        {
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        if (!state.Stopwatch.IsRunning)
        {
            return;
        }

        var elapsed = state.Stopwatch.Elapsed;
        var lapTime = elapsed - state.LastLapCheckpoint;
        if (lapTime <= TimeSpan.Zero)
        {
            return;
        }

        state.LapRecords.Add(new TrainingLapTimeListItem
        {
            Nummer = state.LapRecords.Count + 1,
            Rundenzeit = lapTime,
            RundenzeitText = FormatTrainingTime(lapTime),
            ZeitstrafeSekunden = 0d,
            Pylonen = 0,
            Tore = 0,
            Ungueltig = false
        });
        var lastLap = state.LapRecords[^1];
        lastLap.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lastLap);
        state.LastLapCheckpoint = elapsed;

        var roundsTarget = _selectedTrainingDetailId is null
            ? 0
            : GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        if (roundsTarget > 0 && state.LapRecords.Count >= roundsTarget)
        {
            state.Stopwatch.Stop();
            if (_trainingStopwatchTimer.IsEnabled)
            {
                _trainingStopwatchTimer.Stop();
            }
        }

        RefreshTrainingLapTimesTable();
        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    private void ClearTrainingStint_OnClick(object sender, RoutedEventArgs e)
    {
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
        if (_trainingStopwatchContext is null)
        {
            return;
        }

        if (_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state))
        {
            state.Stopwatch.Reset();
            state.LapRecords.Clear();
            state.LastLapCheckpoint = TimeSpan.Zero;
        }

        if (_trainingStopwatchTimer.IsEnabled)
        {
            _trainingStopwatchTimer.Stop();
        }

        RefreshTrainingLapTimesTable();
        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    private void UpdateTrainingStopwatchDisplay()
    {
        if (_trainingStopwatchContext is null)
        {
            TrainingStopwatchTextBlock.Text = "00.000";
            UpdateTrainingLapProgressDisplay();
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        var currentLapElapsed = state.Stopwatch.Elapsed - state.LastLapCheckpoint;
        if (currentLapElapsed < TimeSpan.Zero)
        {
            currentLapElapsed = TimeSpan.Zero;
        }

        TrainingStopwatchTextBlock.Text = FormatTrainingTime(currentLapElapsed);
        UpdateTrainingLapProgressDisplay();
    }

    private void UpdateTrainingStopwatchButtonsState()
    {
        if (_trainingStopwatchContext is null)
        {
            TrainingStopwatchStartButton.IsEnabled = false;
            TrainingStopwatchStopButton.IsEnabled = false;
            TrainingStopwatchStopButton.Content = "Runde";
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        var roundsTarget = _selectedTrainingDetailId is null
            ? 0
            : GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        var isLastLap = roundsTarget > 0 && state.LapRecords.Count >= roundsTarget - 1;
        var stintFinished = roundsTarget > 0 && state.LapRecords.Count >= roundsTarget;

        TrainingStopwatchStartButton.IsEnabled = !state.Stopwatch.IsRunning && !stintFinished;
        TrainingStopwatchStopButton.IsEnabled = state.Stopwatch.IsRunning;
        TrainingStopwatchStopButton.Content = isLastLap ? "Stop" : "Runde";
    }

    private void LapNumericAdjust_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not TrainingLapTimeListItem lapItem)
        {
            return;
        }

        var tagParts = button.Tag?.ToString()?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tagParts is null || tagParts.Length != 2 || !int.TryParse(tagParts[1], out var delta))
        {
            return;
        }

        switch (tagParts[0])
        {
            case "Pylonen":
                lapItem.Pylonen = Math.Max(0, lapItem.Pylonen + delta);
                lapItem.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lapItem);
                UpdateTrainingLapSummaryDisplay();
                break;
            case "Tore":
                lapItem.Tore = Math.Max(0, lapItem.Tore + delta);
                lapItem.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lapItem);
                UpdateTrainingLapSummaryDisplay();
                break;
        }
    }

    private void LapInvalidCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TrainingLapTimeListItem lap)
        {
            lap.Ungueltig = checkBox.IsChecked == true;
        }

        UpdateTrainingLapSummaryDisplay();
    }

    private async void OpenAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null)
        {
            MessageBox.Show("Bitte zuerst ein Training auswaehlen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var assignedDrivers = await dbContext.FahrerImTrainings
                .AsNoTracking()
                .Where(x => x.TrainingId == _selectedTrainingDetailId.Value)
                .Select(x => new { x.FahrerId, x.Reihenfolge })
                .ToListAsync();

            var assignedOrderMap = assignedDrivers.ToDictionary(x => x.FahrerId, x => x.Reihenfolge);

            var availableDrivers = await dbContext.Fahrer
                .AsNoTracking()
                .Include(x => x.Verein)
                .OrderBy(x => x.Vorname)
                .ThenBy(x => x.Nachname)
                .Select(x => new
                {
                    x.Id,
                    DisplayName = string.IsNullOrWhiteSpace(x.Nachname)
                        ? $"{x.Vorname} ({x.Verein.Vereinsname})"
                        : $"{x.Vorname} {x.Nachname} ({x.Verein.Vereinsname})"
                })
                .ToListAsync();

            TrainingDriverSelectionItems.Clear();
            foreach (var driver in availableDrivers)
            {
                TrainingDriverSelectionItems.Add(new TrainingDriverSelectionItem
                {
                    FahrerId = driver.Id,
                    DisplayName = driver.DisplayName,
                    IsSelected = assignedOrderMap.ContainsKey(driver.Id),
                    SelectionOrder = assignedOrderMap.TryGetValue(driver.Id, out var reihenfolge) ? reihenfolge : 0
                });
            }

            _trainingDriverSearchTerm = string.Empty;
            _trainingDriverSelectionOrderCounter = TrainingDriverSelectionItems
                .Select(x => x.SelectionOrder)
                .DefaultIfEmpty(0)
                .Max();
            TrainingFahrerSearchTextBox.Text = string.Empty;
            ApplyTrainingDriverSelectionFilter();
            TrainingFahrerDialogOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der verfuegbaren Fahrer fuer das Training.");
            MessageBox.Show("Fahrer konnten nicht geladen werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null)
        {
            CloseTrainingDriverSelectionDialog();
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var existingAssignments = await dbContext.FahrerImTrainings
                .IgnoreQueryFilters()
                .Where(x => x.TrainingId == _selectedTrainingDetailId.Value)
                .ToListAsync();

            var existingMap = existingAssignments.ToDictionary(x => x.FahrerId);
            var selectedInOrder = TrainingDriverSelectionItems
                .Where(x => x.IsSelected)
                .OrderBy(x => x.SelectionOrder)
                .ThenBy(x => x.DisplayName)
                .ToList();

            var reihenfolge = 1;
            foreach (var selected in selectedInOrder)
            {
                existingMap.TryGetValue(selected.FahrerId, out var existing);

                if (existing is null)
                {
                    dbContext.FahrerImTrainings.Add(new FahrerImTraining
                    {
                        TrainingId = _selectedTrainingDetailId.Value,
                        FahrerId = selected.FahrerId,
                        Reihenfolge = reihenfolge
                    });
                    reihenfolge++;
                    continue;
                }

                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAtUtc = null;
                }

                existing.Reihenfolge = reihenfolge;
                reihenfolge++;
            }

            foreach (var selection in TrainingDriverSelectionItems)
            {
                existingMap.TryGetValue(selection.FahrerId, out var existing);

                if (!selection.IsSelected && existing is not null && !existing.IsDeleted)
                {
                    dbContext.FahrerImTrainings.Remove(existing);
                    _trainingDriverEnabledByDriver.Remove((_selectedTrainingDetailId.Value, selection.FahrerId));
                    _trainingKartSelectionByDriver.Remove((_selectedTrainingDetailId.Value, selection.FahrerId));
                    _trainingStintsByDriver.Remove((_selectedTrainingDetailId.Value, selection.FahrerId));
                }
            }

            await dbContext.SaveChangesAsync();

            CloseTrainingDriverSelectionDialog();
            await LoadTrainingStarterListAsync(_selectedTrainingDetailId.Value);
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Hinzufuegen von Fahrern zum Training.");
            MessageBox.Show("Fahrer konnten nicht zum Training hinzugefuegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e)
    {
        CloseTrainingDriverSelectionDialog();
    }

    private void TrainingFahrerSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _trainingDriverSearchTerm = TrainingFahrerSearchTextBox.Text.Trim();
        ApplyTrainingDriverSelectionFilter();
    }

    private void TrainingDriverSelectionCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not TrainingDriverSelectionItem item)
        {
            return;
        }

        var isChecked = checkBox.IsChecked == true;
        item.IsSelected = isChecked;

        if (isChecked)
        {
            if (item.SelectionOrder <= 0)
            {
                _trainingDriverSelectionOrderCounter++;
                item.SelectionOrder = _trainingDriverSelectionOrderCounter;
            }

            return;
        }

        item.SelectionOrder = 0;
    }

    private void ApplyTrainingDriverSelectionFilter()
    {
        var view = CollectionViewSource.GetDefaultView(TrainingDriverSelectionItems);
        if (view is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_trainingDriverSearchTerm))
        {
            view.Filter = null;
            view.Refresh();
            return;
        }

        var term = _trainingDriverSearchTerm;
        view.Filter = item =>
        {
            if (item is not TrainingDriverSelectionItem driver)
            {
                return false;
            }

            return driver.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase);
        };
        view.Refresh();
    }

    private void CloseTrainingDriverSelectionDialog()
    {
        _trainingDriverSearchTerm = string.Empty;
        _trainingDriverSelectionOrderCounter = 0;
        if (TrainingFahrerSearchTextBox is not null)
        {
            TrainingFahrerSearchTextBox.Text = string.Empty;
        }

        var view = CollectionViewSource.GetDefaultView(TrainingDriverSelectionItems);
        if (view is not null)
        {
            view.Filter = null;
            view.Refresh();
        }

        TrainingDriverSelectionItems.Clear();
        TrainingFahrerDialogOverlay.Visibility = Visibility.Collapsed;
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        DragMove();
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LogoImage_OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        LogoImage.Visibility = Visibility.Collapsed;
    }

    private static string FormatSecondsValue(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool TryParseSecondsValue(string input, out double value)
    {
        var normalized = input.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void OpenCreateDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        CreateDisziplinNameTextBox.Text = string.Empty;
        CreateDisziplinTfTextBox.Text = "0";
        CreateDisziplinPfTextBox.Text = "0";
        ShowDisziplinDialog(DisziplinDialogMode.Create);
    }

    private async void SaveCreateDisziplin_OnClick(object sender, RoutedEventArgs e)
    {
        var name = CreateDisziplinNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte einen Namen eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(CreateDisziplinTfTextBox.Text, out var zeitstrafeTorfehler) || zeitstrafeTorfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Torfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(CreateDisziplinPfTextBox.Text, out var zeitstrafePylonenfehler) || zeitstrafePylonenfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Pylonenfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Disziplinen.Add(new Disziplin
            {
                Name = name,
                ZeitstrafeTorfehler = zeitstrafeTorfehler,
                ZeitstrafePylonenfehler = zeitstrafePylonenfehler
            });
            await dbContext.SaveChangesAsync();

            ShowDisziplinDialog(DisziplinDialogMode.None);
            await LoadDisziplinenAsync();
            await LoadLookupDataAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen einer Disziplin.");
            MessageBox.Show("Disziplin konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenEditDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var disziplinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var disziplin = await dbContext.Disziplinen.FirstOrDefaultAsync(x => x.Id == disziplinId);
        if (disziplin is null)
        {
            return;
        }

        _editDisziplinId = disziplin.Id;
        EditDisziplinNameTextBox.Text = disziplin.Name;
        EditDisziplinTfTextBox.Text = FormatSecondsValue(disziplin.ZeitstrafeTorfehler);
        EditDisziplinPfTextBox.Text = FormatSecondsValue(disziplin.ZeitstrafePylonenfehler);
        ShowDisziplinDialog(DisziplinDialogMode.Edit);
    }

    private async void SaveEditDisziplin_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editDisziplinId is null)
        {
            return;
        }

        var name = EditDisziplinNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte einen Namen eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(EditDisziplinTfTextBox.Text, out var zeitstrafeTorfehler) || zeitstrafeTorfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Torfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(EditDisziplinPfTextBox.Text, out var zeitstrafePylonenfehler) || zeitstrafePylonenfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Pylonenfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var disziplin = await dbContext.Disziplinen.FirstOrDefaultAsync(x => x.Id == _editDisziplinId.Value);
            if (disziplin is null)
            {
                ShowDisziplinDialog(DisziplinDialogMode.None);
                await LoadDisziplinenAsync();
                return;
            }

            disziplin.Name = name;
            disziplin.ZeitstrafeTorfehler = zeitstrafeTorfehler;
            disziplin.ZeitstrafePylonenfehler = zeitstrafePylonenfehler;
            await dbContext.SaveChangesAsync();

            _editDisziplinId = null;
            ShowDisziplinDialog(DisziplinDialogMode.None);
            await LoadDisziplinenAsync();
            await LoadLookupDataAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten einer Disziplin.");
            MessageBox.Show("Disziplin konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenDeleteDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var disziplinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var disziplin = await dbContext.Disziplinen.FirstOrDefaultAsync(x => x.Id == disziplinId);
        if (disziplin is null)
        {
            return;
        }

        _deleteDisziplinId = disziplin.Id;
        DeleteDisziplinTextBlock.Text = disziplin.Name;
        ShowDisziplinDialog(DisziplinDialogMode.Delete);
    }

    private async void ConfirmDeleteDisziplin_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteDisziplinId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var disziplin = await dbContext.Disziplinen.FirstOrDefaultAsync(x => x.Id == _deleteDisziplinId.Value);
            if (disziplin is not null)
            {
                dbContext.Disziplinen.Remove(disziplin);
                await dbContext.SaveChangesAsync();
            }

            _deleteDisziplinId = null;
            ShowDisziplinDialog(DisziplinDialogMode.None);
            await LoadDisziplinenAsync();
            await LoadLookupDataAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen einer Disziplin.");
            MessageBox.Show("Disziplin konnte nicht geloescht werden. Sie wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editDisziplinId = null;
        _deleteDisziplinId = null;
        ShowDisziplinDialog(DisziplinDialogMode.None);
    }

    private void ShowDisziplinDialog(DisziplinDialogMode mode)
    {
        DisziplinCreatePage.Visibility = mode == DisziplinDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        DisziplinEditPage.Visibility = mode == DisziplinDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        DisziplinDeletePage.Visibility = mode == DisziplinDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenCreateVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        CreateMitgliedsNummerTextBox.Text = string.Empty;
        CreateVereinsnameTextBox.Text = string.Empty;
        ShowVereinDialog(VereinDialogMode.Create);
    }

    private async void SaveCreateVerein_OnClick(object sender, RoutedEventArgs e)
    {
        var mitgliedsNummer = CreateMitgliedsNummerTextBox.Text.Trim();
        var vereinsname = CreateVereinsnameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(mitgliedsNummer) || string.IsNullOrWhiteSpace(vereinsname))
        {
            MessageBox.Show("Bitte Mitgliedsnummer und Vereinsname ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Vereine.Add(new Verein
            {
                MitgliedsNummer = mitgliedsNummer,
                Vereinsname = vereinsname
            });

            await dbContext.SaveChangesAsync();
            ShowVereinDialog(VereinDialogMode.None);
            await LoadVereineAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Vereins.");
            MessageBox.Show("Verein konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenEditVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var vereinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == vereinId);
        if (verein is null)
        {
            return;
        }

        _editVereinId = verein.Id;
        EditMitgliedsNummerTextBox.Text = verein.MitgliedsNummer;
        EditVereinsnameTextBox.Text = verein.Vereinsname;
        ShowVereinDialog(VereinDialogMode.Edit);
    }

    private async void SaveEditVerein_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editVereinId is null)
        {
            return;
        }

        var mitgliedsNummer = EditMitgliedsNummerTextBox.Text.Trim();
        var vereinsname = EditVereinsnameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(mitgliedsNummer) || string.IsNullOrWhiteSpace(vereinsname))
        {
            MessageBox.Show("Bitte Mitgliedsnummer und Vereinsname ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == _editVereinId.Value);
            if (verein is null)
            {
                ShowVereinDialog(VereinDialogMode.None);
                await LoadVereineAsync();
                return;
            }

            verein.MitgliedsNummer = mitgliedsNummer;
            verein.Vereinsname = vereinsname;
            await dbContext.SaveChangesAsync();

            _editVereinId = null;
            ShowVereinDialog(VereinDialogMode.None);
            await LoadVereineAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Vereins.");
            MessageBox.Show("Verein konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenDeleteVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var vereinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == vereinId);
        if (verein is null)
        {
            return;
        }

        _deleteVereinId = verein.Id;
        DeleteVereinTextBlock.Text = $"{verein.MitgliedsNummer} - {verein.Vereinsname}";
        ShowVereinDialog(VereinDialogMode.Delete);
    }

    private async void ConfirmDeleteVerein_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteVereinId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == _deleteVereinId.Value);
            if (verein is not null)
            {
                dbContext.Vereine.Remove(verein);
                await dbContext.SaveChangesAsync();
            }

            _deleteVereinId = null;
            ShowVereinDialog(VereinDialogMode.None);
            await LoadVereineAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Vereins.");
            MessageBox.Show("Verein konnte nicht geloescht werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editVereinId = null;
        _deleteVereinId = null;
        ShowVereinDialog(VereinDialogMode.None);
    }

    private void ShowVereinDialog(VereinDialogMode mode)
    {
        VereinCreatePage.Visibility = mode == VereinDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        VereinEditPage.Visibility = mode == VereinDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        VereinDeletePage.Visibility = mode == VereinDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OpenCreateFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadLookupDataAsync();
        CreateFahrerVornameTextBox.Text = string.Empty;
        CreateFahrerNachnameTextBox.Text = string.Empty;
        CreateFahrerGeburtsdatumPicker.SelectedDate = null;
        CreateFahrerVereinComboBox.SelectedIndex = -1;
        ShowFahrerDialog(FahrerDialogMode.Create);
    }

    private async void SaveCreateFahrer_OnClick(object sender, RoutedEventArgs e)
    {
        var vorname = CreateFahrerVornameTextBox.Text.Trim();
        var nachname = CreateFahrerNachnameTextBox.Text.Trim();
        var geburtsdatum = CreateFahrerGeburtsdatumPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(CreateFahrerGeburtsdatumPicker.SelectedDate.Value)
            : (DateOnly?)null;
        if (string.IsNullOrWhiteSpace(vorname) || CreateFahrerVereinComboBox.SelectedValue is not int vereinId)
        {
            MessageBox.Show("Bitte Vorname und Verein ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Fahrer.Add(new Fahrer
            {
                Vorname = vorname,
                Nachname = string.IsNullOrWhiteSpace(nachname) ? null : nachname,
                Geburtsdatum = geburtsdatum,
                VereinId = vereinId
            });
            await dbContext.SaveChangesAsync();

            ShowFahrerDialog(FahrerDialogMode.None);
            await LoadFahrerAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Fahrers.");
            MessageBox.Show("Fahrer konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenEditFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var fahrerId))
        {
            return;
        }

        await LoadLookupDataAsync();
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var fahrer = await dbContext.Fahrer.FirstOrDefaultAsync(x => x.Id == fahrerId);
        if (fahrer is null)
        {
            return;
        }

        _editFahrerId = fahrer.Id;
        EditFahrerVornameTextBox.Text = fahrer.Vorname;
        EditFahrerNachnameTextBox.Text = fahrer.Nachname ?? string.Empty;
        EditFahrerGeburtsdatumPicker.SelectedDate = fahrer.Geburtsdatum?.ToDateTime(TimeOnly.MinValue);
        EditFahrerVereinComboBox.SelectedValue = fahrer.VereinId;
        ShowFahrerDialog(FahrerDialogMode.Edit);
    }

    private async void SaveEditFahrer_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editFahrerId is null)
        {
            return;
        }

        var vorname = EditFahrerVornameTextBox.Text.Trim();
        var nachname = EditFahrerNachnameTextBox.Text.Trim();
        var geburtsdatum = EditFahrerGeburtsdatumPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(EditFahrerGeburtsdatumPicker.SelectedDate.Value)
            : (DateOnly?)null;
        if (string.IsNullOrWhiteSpace(vorname) || EditFahrerVereinComboBox.SelectedValue is not int vereinId)
        {
            MessageBox.Show("Bitte Vorname und Verein ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var fahrer = await dbContext.Fahrer.FirstOrDefaultAsync(x => x.Id == _editFahrerId.Value);
            if (fahrer is null)
            {
                ShowFahrerDialog(FahrerDialogMode.None);
                await LoadFahrerAsync();
                return;
            }

            fahrer.Vorname = vorname;
            fahrer.Nachname = string.IsNullOrWhiteSpace(nachname) ? null : nachname;
            fahrer.Geburtsdatum = geburtsdatum;
            fahrer.VereinId = vereinId;
            await dbContext.SaveChangesAsync();

            _editFahrerId = null;
            ShowFahrerDialog(FahrerDialogMode.None);
            await LoadFahrerAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Fahrers.");
            MessageBox.Show("Fahrer konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenDeleteFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var fahrerId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var fahrer = await dbContext.Fahrer.AsNoTracking().FirstOrDefaultAsync(x => x.Id == fahrerId);
        if (fahrer is null)
        {
            return;
        }

        _deleteFahrerId = fahrer.Id;
        DeleteFahrerTextBlock.Text = $"{fahrer.Vorname} {fahrer.Nachname}".Trim();
        ShowFahrerDialog(FahrerDialogMode.Delete);
    }

    private async void ConfirmDeleteFahrer_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteFahrerId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var fahrer = await dbContext.Fahrer.FirstOrDefaultAsync(x => x.Id == _deleteFahrerId.Value);
            if (fahrer is not null)
            {
                dbContext.Fahrer.Remove(fahrer);
                await dbContext.SaveChangesAsync();
            }

            _deleteFahrerId = null;
            ShowFahrerDialog(FahrerDialogMode.None);
            await LoadFahrerAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Fahrers.");
            MessageBox.Show("Fahrer konnte nicht geloescht werden. Er wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editFahrerId = null;
        _deleteFahrerId = null;
        ShowFahrerDialog(FahrerDialogMode.None);
    }

    private void ShowFahrerDialog(FahrerDialogMode mode)
    {
        FahrerCreatePage.Visibility = mode == FahrerDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        FahrerEditPage.Visibility = mode == FahrerDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        FahrerDeletePage.Visibility = mode == FahrerDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OpenCreateTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadLookupDataAsync();
        CreateTrainingNameTextBox.Text = string.Empty;
        CreateTrainingBeschreibungTextBox.Text = string.Empty;
        CreateTrainingZeitpunktPicker.SelectedDate = DateTime.Today;
        CreateTrainingAbgeschlossenCheckBox.IsChecked = false;
        CreateTrainingVereinComboBox.SelectedIndex = -1;
        CreateTrainingDisziplinComboBox.SelectedIndex = -1;
        CreateTrainingWetterComboBox.SelectedIndex = -1;
        ShowTrainingDialog(TrainingDialogMode.Create);
    }

    private async void SaveCreateTraining_OnClick(object sender, RoutedEventArgs e)
    {
        var name = CreateTrainingNameTextBox.Text.Trim();
        var beschreibung = CreateTrainingBeschreibungTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(beschreibung) ||
            !CreateTrainingZeitpunktPicker.SelectedDate.HasValue ||
            CreateTrainingVereinComboBox.SelectedValue is not int vereinId ||
            CreateTrainingDisziplinComboBox.SelectedValue is not int disziplinId ||
            CreateTrainingWetterComboBox.SelectedValue is not int wetterId)
        {
            MessageBox.Show("Bitte alle Felder ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Trainings.Add(new Training
            {
                Name = name,
                Beschreibung = beschreibung,
                Zeitpunkt = DateOnly.FromDateTime(CreateTrainingZeitpunktPicker.SelectedDate.Value),
                TrainingAbgeschlossen = CreateTrainingAbgeschlossenCheckBox.IsChecked == true,
                VereinId = vereinId,
                DisziplinId = disziplinId,
                WetterId = wetterId
            });

            await dbContext.SaveChangesAsync();
            ShowTrainingDialog(TrainingDialogMode.None);
            await LoadTrainingsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Trainings.");
            MessageBox.Show("Training konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenEditTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var trainingId))
        {
            return;
        }

        await LoadLookupDataAsync();
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == trainingId);
        if (training is null)
        {
            return;
        }

        _editTrainingId = training.Id;
        EditTrainingNameTextBox.Text = training.Name;
        EditTrainingBeschreibungTextBox.Text = training.Beschreibung;
        EditTrainingZeitpunktPicker.SelectedDate = training.Zeitpunkt.ToDateTime(TimeOnly.MinValue);
        EditTrainingAbgeschlossenCheckBox.IsChecked = training.TrainingAbgeschlossen;
        EditTrainingVereinComboBox.SelectedValue = training.VereinId;
        EditTrainingDisziplinComboBox.SelectedValue = training.DisziplinId;
        EditTrainingWetterComboBox.SelectedValue = training.WetterId;
        ShowTrainingDialog(TrainingDialogMode.Edit);
    }

    private async void SaveEditTraining_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editTrainingId is null)
        {
            return;
        }

        var name = EditTrainingNameTextBox.Text.Trim();
        var beschreibung = EditTrainingBeschreibungTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(beschreibung) ||
            !EditTrainingZeitpunktPicker.SelectedDate.HasValue ||
            EditTrainingVereinComboBox.SelectedValue is not int vereinId ||
            EditTrainingDisziplinComboBox.SelectedValue is not int disziplinId ||
            EditTrainingWetterComboBox.SelectedValue is not int wetterId)
        {
            MessageBox.Show("Bitte alle Felder ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == _editTrainingId.Value);
            if (training is null)
            {
                ShowTrainingDialog(TrainingDialogMode.None);
                await LoadTrainingsAsync();
                return;
            }

            training.Name = name;
            training.Beschreibung = beschreibung;
            training.Zeitpunkt = DateOnly.FromDateTime(EditTrainingZeitpunktPicker.SelectedDate.Value);
            training.TrainingAbgeschlossen = EditTrainingAbgeschlossenCheckBox.IsChecked == true;
            training.VereinId = vereinId;
            training.DisziplinId = disziplinId;
            training.WetterId = wetterId;
            await dbContext.SaveChangesAsync();

            _editTrainingId = null;
            ShowTrainingDialog(TrainingDialogMode.None);
            await LoadTrainingsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Trainings.");
            MessageBox.Show("Training konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenDeleteTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var trainingId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var training = await dbContext.Trainings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == trainingId);
        if (training is null)
        {
            return;
        }

        _deleteTrainingId = training.Id;
        DeleteTrainingTextBlock.Text = training.Name;
        ShowTrainingDialog(TrainingDialogMode.Delete);
    }

    private async void ConfirmDeleteTraining_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteTrainingId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == _deleteTrainingId.Value);
            if (training is not null)
            {
                dbContext.Trainings.Remove(training);
                await dbContext.SaveChangesAsync();
            }

            _deleteTrainingId = null;
            ShowTrainingDialog(TrainingDialogMode.None);
            await LoadTrainingsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Trainings.");
            MessageBox.Show("Training konnte nicht geloescht werden. Es wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editTrainingId = null;
        _deleteTrainingId = null;
        ShowTrainingDialog(TrainingDialogMode.None);
    }

    private void ShowTrainingDialog(TrainingDialogMode mode)
    {
        TrainingCreatePage.Visibility = mode == TrainingDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        TrainingEditPage.Visibility = mode == TrainingDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        TrainingDeletePage.Visibility = mode == TrainingDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OpenCreateKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadLookupDataAsync();
        CreateKartNameTextBox.Text = string.Empty;
        CreateKartMotorTextBox.Text = string.Empty;
        CreateKartChassisTextBox.Text = string.Empty;
        CreateKartVereinComboBox.SelectedIndex = -1;
        CreateKartDisziplinComboBox.SelectedIndex = -1;
        ShowKartDialog(KartDialogMode.Create);
    }

    private async void SaveCreateKart_OnClick(object sender, RoutedEventArgs e)
    {
        if (CreateKartVereinComboBox.SelectedValue is not int vereinId ||
            CreateKartDisziplinComboBox.SelectedValue is not int disziplinId)
        {
            MessageBox.Show("Bitte Verein und Disziplin auswaehlen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Karts.Add(new Kart
            {
                Name = string.IsNullOrWhiteSpace(CreateKartNameTextBox.Text) ? null : CreateKartNameTextBox.Text.Trim(),
                Motor = string.IsNullOrWhiteSpace(CreateKartMotorTextBox.Text) ? null : CreateKartMotorTextBox.Text.Trim(),
                Chassis = string.IsNullOrWhiteSpace(CreateKartChassisTextBox.Text) ? null : CreateKartChassisTextBox.Text.Trim(),
                VereinId = vereinId,
                DisziplinId = disziplinId
            });

            await dbContext.SaveChangesAsync();
            ShowKartDialog(KartDialogMode.None);
            await LoadKartsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Karts.");
            MessageBox.Show("Kart konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenEditKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var kartId))
        {
            return;
        }

        await LoadLookupDataAsync();
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var kart = await dbContext.Karts.FirstOrDefaultAsync(x => x.Id == kartId);
        if (kart is null)
        {
            return;
        }

        _editKartId = kart.Id;
        EditKartNameTextBox.Text = kart.Name ?? string.Empty;
        EditKartMotorTextBox.Text = kart.Motor ?? string.Empty;
        EditKartChassisTextBox.Text = kart.Chassis ?? string.Empty;
        EditKartVereinComboBox.SelectedValue = kart.VereinId;
        EditKartDisziplinComboBox.SelectedValue = kart.DisziplinId;
        ShowKartDialog(KartDialogMode.Edit);
    }

    private async void SaveEditKart_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editKartId is null)
        {
            return;
        }

        if (EditKartVereinComboBox.SelectedValue is not int vereinId ||
            EditKartDisziplinComboBox.SelectedValue is not int disziplinId)
        {
            MessageBox.Show("Bitte Verein und Disziplin auswaehlen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var kart = await dbContext.Karts.FirstOrDefaultAsync(x => x.Id == _editKartId.Value);
            if (kart is null)
            {
                ShowKartDialog(KartDialogMode.None);
                await LoadKartsAsync();
                return;
            }

            kart.Name = string.IsNullOrWhiteSpace(EditKartNameTextBox.Text) ? null : EditKartNameTextBox.Text.Trim();
            kart.Motor = string.IsNullOrWhiteSpace(EditKartMotorTextBox.Text) ? null : EditKartMotorTextBox.Text.Trim();
            kart.Chassis = string.IsNullOrWhiteSpace(EditKartChassisTextBox.Text) ? null : EditKartChassisTextBox.Text.Trim();
            kart.VereinId = vereinId;
            kart.DisziplinId = disziplinId;
            await dbContext.SaveChangesAsync();

            _editKartId = null;
            ShowKartDialog(KartDialogMode.None);
            await LoadKartsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Karts.");
            MessageBox.Show("Kart konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenDeleteKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var kartId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var kart = await dbContext.Karts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == kartId);
        if (kart is null)
        {
            return;
        }

        _deleteKartId = kart.Id;
        DeleteKartTextBlock.Text = string.IsNullOrWhiteSpace(kart.Name) ? $"Kart #{kart.Id}" : kart.Name;
        ShowKartDialog(KartDialogMode.Delete);
    }

    private async void ConfirmDeleteKart_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteKartId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var kart = await dbContext.Karts.FirstOrDefaultAsync(x => x.Id == _deleteKartId.Value);
            if (kart is not null)
            {
                dbContext.Karts.Remove(kart);
                await dbContext.SaveChangesAsync();
            }

            _deleteKartId = null;
            ShowKartDialog(KartDialogMode.None);
            await LoadKartsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Karts.");
            MessageBox.Show("Kart konnte nicht geloescht werden. Es wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editKartId = null;
        _deleteKartId = null;
        ShowKartDialog(KartDialogMode.None);
    }

    private void ShowKartDialog(KartDialogMode mode)
    {
        KartCreatePage.Visibility = mode == KartDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        KartEditPage.Visibility = mode == KartDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        KartDeletePage.Visibility = mode == KartDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenCreateWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        CreateWetterNameTextBox.Text = string.Empty;
        ShowWetterDialog(WetterDialogMode.Create);
    }

    private async void SaveCreateWetter_OnClick(object sender, RoutedEventArgs e)
    {
        var name = CreateWetterNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte eine Wetterbezeichnung eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Wetterlagen.Add(new Wetter { Bezeichnung = name });
            await dbContext.SaveChangesAsync();

            ShowWetterDialog(WetterDialogMode.None);
            await LoadWetterAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Wettereintrags.");
            MessageBox.Show("Wetter konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenEditWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var wetterId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var wetter = await dbContext.Wetterlagen.FirstOrDefaultAsync(x => x.Id == wetterId);
        if (wetter is null)
        {
            return;
        }

        _editWetterId = wetter.Id;
        EditWetterNameTextBox.Text = wetter.Bezeichnung;
        ShowWetterDialog(WetterDialogMode.Edit);
    }

    private async void SaveEditWetter_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editWetterId is null)
        {
            return;
        }

        var name = EditWetterNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte eine Wetterbezeichnung eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var wetter = await dbContext.Wetterlagen.FirstOrDefaultAsync(x => x.Id == _editWetterId.Value);
            if (wetter is null)
            {
                ShowWetterDialog(WetterDialogMode.None);
                await LoadWetterAsync();
                return;
            }

            wetter.Bezeichnung = name;
            await dbContext.SaveChangesAsync();

            _editWetterId = null;
            ShowWetterDialog(WetterDialogMode.None);
            await LoadWetterAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Wettereintrags.");
            MessageBox.Show("Wetter konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenDeleteWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var wetterId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var wetter = await dbContext.Wetterlagen.AsNoTracking().FirstOrDefaultAsync(x => x.Id == wetterId);
        if (wetter is null)
        {
            return;
        }

        _deleteWetterId = wetter.Id;
        DeleteWetterTextBlock.Text = wetter.Bezeichnung;
        ShowWetterDialog(WetterDialogMode.Delete);
    }

    private async void ConfirmDeleteWetter_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteWetterId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var wetter = await dbContext.Wetterlagen.FirstOrDefaultAsync(x => x.Id == _deleteWetterId.Value);
            if (wetter is not null)
            {
                dbContext.Wetterlagen.Remove(wetter);
                await dbContext.SaveChangesAsync();
            }

            _deleteWetterId = null;
            ShowWetterDialog(WetterDialogMode.None);
            await LoadWetterAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Wettereintrags.");
            MessageBox.Show("Wetter konnte nicht geloescht werden. Es wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editWetterId = null;
        _deleteWetterId = null;
        ShowWetterDialog(WetterDialogMode.None);
    }

    private void ShowWetterDialog(WetterDialogMode mode)
    {
        WetterCreatePage.Visibility = mode == WetterDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        WetterEditPage.Visibility = mode == WetterDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        WetterDeletePage.Visibility = mode == WetterDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void SyncNow_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_syncNeeded)
        {
            return;
        }

        _syncInProgress = true;
        UpdateSyncButtonVisualState();

        try
        {
            var result = await _dataSyncService.SyncBidirectionalAsync();
            FooterSyncStateTextBlock.Text = result.Message;
            FooterSyncStateTextBlock.ToolTip = result.Message;

            await LoadDisziplinenAsync();
            await LoadVereineAsync();
            await LoadTrainingsAsync();
            await LoadFahrerAsync();
            await LoadKartsAsync();
            await LoadWetterAsync();
            await LoadLookupDataAsync();

            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Synchronisierung fehlgeschlagen.");
            FooterSyncStateTextBlock.Text = "Synchronisierung fehlgeschlagen.";
            FooterSyncStateTextBlock.ToolTip = ex.Message;
            _syncNeeded = true;
            UpdateSyncButtonVisualState();
        }
        finally
        {
            _syncInProgress = false;
            UpdateSyncButtonVisualState();
        }
    }

    public sealed class VereinListItem
    {
        public required int Id { get; init; }

        public required string MitgliedsNummer { get; init; }

        public required string Vereinsname { get; init; }
    }

    public sealed class DisziplinListItem
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required double ZeitstrafeTorfehler { get; init; }

        public required double ZeitstrafePylonenfehler { get; init; }

        public required string ZeitstrafeTorfehlerText { get; init; }

        public required string ZeitstrafePylonenfehlerText { get; init; }
    }

    public sealed class FahrerListItem
    {
        public required int Id { get; init; }

        public required int VereinId { get; init; }

        public required string Vorname { get; init; }

        public required string Nachname { get; init; }

        public required DateOnly? Geburtsdatum { get; init; }

        public required string GeburtsdatumText { get; init; }

        public required string VereinName { get; init; }
    }

    public sealed class TrainingListItem
    {
        public required int Id { get; init; }

        public required int VereinId { get; init; }

        public required int DisziplinId { get; init; }

        public required int WetterId { get; init; }

        public required string Name { get; init; }

        public required string Beschreibung { get; init; }

        public required DateOnly Zeitpunkt { get; init; }

        public required string ZeitpunktText { get; init; }

        public required bool TrainingAbgeschlossen { get; init; }

        public required string TrainingAbgeschlossenText { get; init; }

        public required string VereinName { get; init; }

        public required string DisziplinName { get; init; }

        public required string WetterName { get; init; }
    }

    public sealed class TrainingStarterListItem
    {
        public int Nummer { get; set; }

        public required int FahrerId { get; init; }

        public required int Reihenfolge { get; init; }

        public bool IsAktiv { get; set; }

        public bool FahrerFaehrt { get; set; }

        public int? KartId { get; set; }

        public required string Vorname { get; init; }

        public required string Nachname { get; init; }

        public required string VereinName { get; init; }
    }

    public sealed class TrainingLapTimeListItem : INotifyPropertyChanged
    {
        private double _zeitstrafeSekunden;
        private int _pylonen;
        private int _tore;
        private bool _ungueltig;

        public int Nummer { get; set; }

        public required TimeSpan Rundenzeit { get; init; }

        public required string RundenzeitText { get; init; }

        public double ZeitstrafeSekunden
        {
            get => _zeitstrafeSekunden;
            set
            {
                var normalized = Math.Round(Math.Max(0d, value), 3, MidpointRounding.AwayFromZero);
                if (_zeitstrafeSekunden == normalized)
                {
                    return;
                }

                _zeitstrafeSekunden = normalized;
                OnPropertyChanged();
            }
        }

        public int Pylonen
        {
            get => _pylonen;
            set
            {
                var normalized = Math.Max(0, value);
                if (_pylonen == normalized)
                {
                    return;
                }

                _pylonen = normalized;
                OnPropertyChanged();
            }
        }

        public int Tore
        {
            get => _tore;
            set
            {
                var normalized = Math.Max(0, value);
                if (_tore == normalized)
                {
                    return;
                }

                _tore = normalized;
                OnPropertyChanged();
            }
        }

        public bool Ungueltig
        {
            get => _ungueltig;
            set
            {
                if (_ungueltig == value)
                {
                    return;
                }

                _ungueltig = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class TrainingFastestLapListItem
    {
        public int Position { get; init; }

        public required string Fahrer { get; init; }

        public required string Kart { get; init; }

        public required string RundenzeitText { get; init; }

        public required string DiffText { get; init; }

        public required string StrafenText { get; init; }

        public required string ZeitpunktText { get; init; }

        public int Runden { get; init; }
    }

    private sealed class TrainingStintState
    {
        public Stopwatch Stopwatch { get; } = new();

        public List<TrainingLapTimeListItem> LapRecords { get; } = [];

        public TimeSpan LastLapCheckpoint { get; set; } = TimeSpan.Zero;
    }

    public sealed class KartListItem
    {
        public required int Id { get; init; }

        public required int VereinId { get; init; }

        public required int DisziplinId { get; init; }

        public required string Name { get; init; }

        public required string Motor { get; init; }

        public required string Chassis { get; init; }

        public required string VereinName { get; init; }

        public required string DisziplinName { get; init; }
    }

    public sealed class WetterListItem
    {
        public required int Id { get; init; }

        public required string Name { get; init; }
    }

    public sealed class LookupItem
    {
        public required int Id { get; init; }

        public required string Name { get; init; }
    }

    public sealed class LocalUiSettings
    {
        public int DefaultRundenanzahlProStint { get; set; } = 10;

        public Dictionary<int, int> TrainingRundenanzahlOverrides { get; set; } = [];
    }

    public sealed class TrainingDriverSelectionItem
    {
        public required int FahrerId { get; init; }

        public required string DisplayName { get; init; }

        public bool IsSelected { get; set; }

        public int SelectionOrder { get; set; }
    }

    private enum DisziplinDialogMode
    {
        None,
        Create,
        Edit,
        Delete
    }

    private enum VereinDialogMode
    {
        None,
        Create,
        Edit,
        Delete
    }

    private enum FahrerDialogMode
    {
        None,
        Create,
        Edit,
        Delete
    }

    private enum TrainingDialogMode
    {
        None,
        Create,
        Edit,
        Delete
    }

    private enum KartDialogMode
    {
        None,
        Create,
        Edit,
        Delete
    }

    private enum WetterDialogMode
    {
        None,
        Create,
        Edit,
        Delete
    }
}
