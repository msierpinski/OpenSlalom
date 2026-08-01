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
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;

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
    private const string TrainingStatisticsTagPrefix = "TrainingStatistics:";
    private static readonly HashSet<string> SettingsSubmenuTags = ["Vereine", "Fahrer", "Disziplin", "Karts", "Wetter"];
    private const int WmGetMinMaxInfo = 0x0024;
    private const int MonitorDefaultToNearest = 0x00000002;
    private static readonly HashSet<string> AllowedLogoExtensions = [".bmp", ".jpg", ".jpeg", ".png"];

    private readonly IDbContextFactory<LocalOpenSlalomDbContext> _localDbContextFactory;
    private readonly IDbContextFactory<OpenSlalomDbContext> _remoteMigrationDbContextFactory;
    private readonly IDbContextFactory<RemoteOpenSlalomDbContext> _remoteDbContextFactory;
    private readonly DataSyncService _dataSyncService;
    private readonly DatabaseRuntimeInfo _databaseRuntimeInfo;
    private readonly string _uiSettingsFilePath;
    private string _selectedMenuTag = "Startseite";
    private bool _syncInProgress;
    private bool _syncNeeded;
    private bool _isSidebarCollapsed;
    private int? _editDisziplinId;
    private int? _deleteDisziplinId;
    private int? _editVereinId;
    private int? _deleteVereinId;
    private byte[]? _createVereinLogoBytes;
    private byte[]? _editVereinLogoBytes;
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
    public ObservableCollection<TrainingStatisticsBestLapListItem> TrainingStatisticsBestLapItems { get; } = new();
    public ObservableCollection<TrainingStatisticsDriverSectionItem> TrainingStatisticsDriverSections { get; } = new();
    public ObservableCollection<DriverStatisticsListItem> DriverStatisticsItems { get; } = new();
    public ObservableCollection<TrainingDriverSelectionItem> TrainingDriverSelectionItems { get; } = new();
    public ObservableCollection<LookupItem> KartLookupItems { get; } = new();
    public ObservableCollection<LookupItem> VereinLookupItems { get; } = new();
    public ObservableCollection<LookupItem> DisziplinLookupItems { get; } = new();
    public ObservableCollection<LookupItem> WetterLookupItems { get; } = new();
    public ObservableCollection<CreateDisziplinAltersklasseItem> CreateDisziplinAltersklassenItems { get; } = new();
    public ObservableCollection<EditDisziplinAltersklasseItem> EditDisziplinAltersklassenItems { get; } = new();

    public MainWindow(
        IDbContextFactory<LocalOpenSlalomDbContext> localDbContextFactory,
        IDbContextFactory<OpenSlalomDbContext> remoteMigrationDbContextFactory,
        IDbContextFactory<RemoteOpenSlalomDbContext> remoteDbContextFactory,
        DataSyncService dataSyncService,
        DatabaseRuntimeInfo databaseRuntimeInfo)
    {
        _localDbContextFactory = localDbContextFactory;
        _remoteMigrationDbContextFactory = remoteMigrationDbContextFactory;
        _remoteDbContextFactory = remoteDbContextFactory;
        _dataSyncService = dataSyncService;
        _databaseRuntimeInfo = databaseRuntimeInfo;
        _uiSettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenSlalom",
            "ui-settings.json");
        InitializeComponent();
        TrainingsViewControl.Host = this;
        StartseiteView.Host = this;
        MeisterschaftenView.Host = this;
        VereineViewControl.Host = this;
        DisziplinenViewControl.Host = this;
        FahrerViewControl.Host = this;
        KartsViewControl.Host = this;
        WetterViewControl.Host = this;
        StatistikenView.Host = this;
        EinstellungenView.Host = this;
        WindowState = WindowState.Maximized;
        DataContext = this;
        ConfigureMenuButtons();
        SetVersionText();
        _trainingStopwatchTimer.Tick += TrainingStopwatchTimer_OnTick;
        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        NavigateTo("Startseite");
        StateChanged += OnWindowStateChanged;
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        UpdateWindowChrome();
        UpdateMaximizeButtonIcon();
        UpdateSidebarState();
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

}
