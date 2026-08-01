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

public partial class MainWindow
{
    public sealed class VereinListItem
    {
        public required int Id { get; init; }

        public required string MitgliedsNummer { get; init; }

        public required string Vereinsname { get; init; }

        public required string Postleitzahl { get; init; }

        public required string Ort { get; init; }

        public required string Adresse { get; init; }

        public required ImageSource? LogoPreview { get; init; }
    }

    public sealed class DisziplinListItem
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required double ZeitstrafeTorfehler { get; init; }

        public required double ZeitstrafePylonenfehler { get; init; }

        public required string ZeitstrafeTorfehlerText { get; init; }

        public required string ZeitstrafePylonenfehlerText { get; init; }

        public required string AltersklassenText { get; init; }
    }

    public sealed class CreateDisziplinAltersklasseItem
    {
        public required string Bezeichnung { get; init; }

        public required int AlterVon { get; init; }

        public required int? AlterBis { get; init; }

        public required string AlterBisText { get; init; }
    }

    public sealed class EditDisziplinAltersklasseItem
    {
        public required string Bezeichnung { get; init; }

        public required int AlterVon { get; init; }

        public required int? AlterBis { get; init; }

        public required string AlterBisText { get; init; }
    }

    public sealed class FahrerListItem
    {
        public required int Id { get; init; }

        public required int VereinId { get; init; }

        public required string Geschlecht { get; init; }

        public required string GeschlechtIconPath { get; init; }

        public required string Vorname { get; init; }

        public required string Nachname { get; init; }

        public required string MitgliedsNummer { get; init; }

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

        public required int Reihenfolge { get; set; }

        public bool IsAktiv { get; set; }

        public bool IsAktivZweiteZeitnahme { get; set; }

        public bool IsBeingDragged { get; set; }

        public bool FahrerFaehrt { get; set; }

        public int? KartId { get; set; }

        public required string Vorname { get; init; }

        public required string Nachname { get; init; }

        public required string Altersklasse { get; init; }

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

        public required string Altersklasse { get; init; }

        public required string Kart { get; init; }

        public required string RundenzeitText { get; init; }

        public required string DiffText { get; init; }

        public required string StrafenText { get; init; }

        public required string ZeitpunktText { get; init; }

        public int Runden { get; init; }
    }

    public sealed class TrainingStoredStintDriverListItem
    {
        public required int FahrerId { get; init; }

        public required string Fahrer { get; init; }
    }

    public sealed class TrainingStoredStintListItem
    {
        public required int StintId { get; init; }

        public required string Titel { get; init; }

        public required string ZeitpunktText { get; init; }

        public required string Kart { get; init; }

        public required string Altersklasse { get; init; }

        public string GesamtzeitText { get; set; } = "-";

        public string DurchschnittszeitText { get; set; } = "-";

        public ObservableCollection<TrainingStoredStintLapListItem> Runden { get; init; } = [];
    }

    public sealed class TrainingStoredStintLapListItem
    {
        public required int RundenId { get; init; }

        public required int StintId { get; init; }

        public required int Runde { get; init; }

        public required double RundenzeitSekunden { get; init; }

        public required string RundenzeitText { get; init; }

        public double ZeitstrafeSekunden { get; set; }

        public int Pylonen { get; set; }

        public int Tore { get; set; }

        public bool Ungueltig { get; set; }
    }

    public sealed class TrainingStatisticsBestLapListItem
    {
        public int Position { get; init; }

        public required string Klasse { get; init; }

        public required string Fahrer { get; init; }

        public required string Kart { get; init; }

        public required string Bestzeit { get; init; }

        public required string Abstand { get; init; }

        public required string Durchschnittszeit { get; init; }

        public int GefahreneRunden { get; init; }

        public required string ZeitpunktLetzteFahrt { get; init; }
    }

    public sealed class TrainingStatisticsDriverSectionItem
    {
        public required int FahrerId { get; init; }

        public required string Titel { get; init; }

        public ObservableCollection<TrainingStatisticsDriverLapItem> LapItems { get; init; } = [];
    }

    public sealed class TrainingStatisticsDriverLapItem
    {
        public int Nummer { get; init; }

        public int Stint { get; init; }

        public int Runde { get; init; }

        public required string Kart { get; init; }

        public required string Zeit { get; init; }

        public required double StrafeSekunden { get; init; }

        public required string StrafeText { get; init; }

        public int P { get; init; }

        public int T { get; init; }

        public required string Zeitpunkt { get; init; }
    }

    private sealed class TrainingStintState
    {
        public Stopwatch Stopwatch { get; } = new();

        public List<TrainingLapTimeListItem> LapRecords { get; } = [];

        public TimeSpan LastLapCheckpoint { get; set; } = TimeSpan.Zero;

        public bool IsFinished { get; set; }
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

    public sealed class DriverStatisticsListItem
    {
        public required string Fahrer { get; init; }

        public required string Fahrzeit { get; init; }

        public int Trainings { get; init; }

        public int Runden { get; init; }

        public required string FehlerfreieRunden { get; init; }

        public int Stints { get; init; }

        public int Pylonenfehler { get; init; }

        public int Torfehler { get; init; }

        public required string DurchschnittPylonenProRunde { get; init; }

        public required string DurchschnittTorfehlerProRunde { get; init; }
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

        public Dictionary<int, bool> TrainingSollrundenUeberschreitenOverrides { get; set; } = [];

        public Dictionary<int, bool> TrainingZweiteZeitnahmeOverrides { get; set; } = [];
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
