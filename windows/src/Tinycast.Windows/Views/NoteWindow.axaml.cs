using Avalonia.Controls;

namespace Tinycast.Windows;

public partial class NoteWindow : Window
{
    readonly AppCore _core;
    readonly Note _note;

    public NoteWindow() : this(AppCore.Shared, new Note()) { }

    public NoteWindow(AppCore core, Note note)
    {
        _core = core;
        _note = note;
        InitializeComponent();
        TitleBox.Text = note.Title;
        BodyBox.Text = note.Body;
        Opened += (_, _) => AcrylicSurface.Apply(this);
        TitleBox.LostFocus += (_, _) => Save();
        BodyBox.LostFocus += (_, _) => Save();
        Closed += (_, _) => Save();
    }

    void Save()
    {
        _note.Title = string.IsNullOrWhiteSpace(TitleBox.Text) ? "Untitled" : TitleBox.Text;
        _note.Body = BodyBox.Text ?? "";
        _core.SaveNote(_note);
        Title = _note.Title;
    }
}
