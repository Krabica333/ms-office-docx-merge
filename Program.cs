using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OpenXmlPowerTools;
namespace DocxMerge;
public static class Program { [STAThread] public static void Main()=>new Application().Run(new MainWindow()); }
public sealed class MainWindow:Window
{
    readonly ObservableCollection<string> documents=new();
    readonly Grid layout=new(); readonly ListBox list=new(); readonly Button merge=new();
    readonly TextBlock status=new(),dropTitle=new(); readonly Border dropArea=new();
    readonly SolidColorBrush dropBackground=new(Color.FromRgb(255,255,255));
    readonly SolidColorBrush dropBorder=new(Color.FromRgb(217,225,239));
    string? dragged; Point dragStart; bool busy;
    static Brush Brush(string color)=>(Brush)new BrushConverter().ConvertFromString(color)!;
    public MainWindow()
    {
        Title="Zlúčenie dokumentov";Width=620;Height=650;MinWidth=500;MinHeight=510;
        WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Background=Brush("#F5F7FB");Foreground=Brush("#172033");FontFamily=new FontFamily("Segoe UI");FontSize=14;
        layout.Margin=new Thickness(30);Content=layout;
        foreach(var size in new[]{GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto})layout.RowDefinitions.Add(new RowDefinition{Height=size});
        var header=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,24)};
        try {var image=new BitmapImage(new Uri("pack://application:,,,/logo.png"));Icon=image;header.Children.Add(new Image{Source=image,Width=52,Height=52,Stretch=Stretch.Uniform,Margin=new Thickness(0,0,16,0)});}catch{}
        var titles=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
        titles.Children.Add(new TextBlock{Text="Zlúčenie dokumentov",FontSize=27,FontWeight=FontWeights.SemiBold});
        titles.Children.Add(new TextBlock{Text="Viac dokumentov. Jeden súbor.",Foreground=Brush("#667085"),Margin=new Thickness(0,6,0,0)});
        header.Children.Add(titles);layout.Children.Add(header);
        dropArea.Background=dropBackground;dropArea.BorderBrush=dropBorder;dropArea.BorderThickness=new Thickness(2);
        dropArea.CornerRadius=new CornerRadius(18);dropArea.Padding=new Thickness(22);dropArea.Margin=new Thickness(0,0,0,18);
        var chooser=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center};
        chooser.Children.Add(new TextBlock{Text="DOCX",FontWeight=FontWeights.Bold,Foreground=Brush("#4263EB"),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,12)});
        dropTitle.Text="Presuňte sem dokumenty";dropTitle.FontSize=20;dropTitle.FontWeight=FontWeights.SemiBold;dropTitle.TextAlignment=TextAlignment.Center;
        chooser.Children.Add(dropTitle);
        chooser.Children.Add(new TextBlock{Text="alebo ich vyberte z počítača",Foreground=Brush("#667085"),TextAlignment=TextAlignment.Center,Margin=new Thickness(0,7,0,16)});
        var add=NewButton("Pridať dokumenty",false);add.HorizontalAlignment=HorizontalAlignment.Center;add.Click+=(_,_)=>Browse();chooser.Children.Add(add);
        dropArea.Child=chooser;Grid.SetRow(dropArea,1);layout.Children.Add(dropArea);
        list.ItemsSource=documents;list.Background=Brush("#FFFFFF");list.BorderBrush=Brush("#E5EAF4");list.BorderThickness=new Thickness(1);list.Padding=new Thickness(8);
        list.HorizontalContentAlignment=HorizontalAlignment.Stretch;
        list.ItemTemplate=CreateRowTemplate();Grid.SetRow(list,2);layout.Children.Add(list);
        var bottom=new StackPanel{Margin=new Thickness(0,16,0,0)};
        status.Foreground=Brush("#667085");status.TextWrapping=TextWrapping.Wrap;status.TextAlignment=TextAlignment.Center;status.Margin=new Thickness(0,0,0,12);bottom.Children.Add(status);
        ApplyButtonStyle(merge,"Zlúčiť a uložiť",true);merge.Height=49;merge.Click+=async(_,_)=>await MergeAsync();bottom.Children.Add(merge);
        bottom.Children.Add(new TextBlock{Text="Pôvodné dokumenty zostanú nezmenené.",Foreground=Brush("#98A2B3"),FontSize=12,TextAlignment=TextAlignment.Center,Margin=new Thickness(0,12,0,0)});
        bottom.Children.Add(new TextBlock{Text="© 2026 Seb Matt www.krabicahub.xyz",Foreground=Brush("#98A2B3"),FontSize=11,TextAlignment=TextAlignment.Center,Margin=new Thickness(0,15,0,0)});
        Grid.SetRow(bottom,3);layout.Children.Add(bottom);
        AllowDrop=true;
        PreviewDragOver+=OnDragOver;
        PreviewDrop+=OnDrop;
        DragLeave+=(_,e)=>{if(!IsMouseOver)AnimateDrop(false);};
        list.PreviewMouseLeftButtonDown+=OnMouseDown;
        list.PreviewMouseMove+=OnMouseMove;
        Closing+=(_,e)=>{if(busy)e.Cancel=true;};
        Loaded+=(_,_)=>{layout.Opacity=0;layout.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(300)));};
        UpdateCount();
    }
    static Button NewButton(string text,bool primary){var button=new Button();ApplyButtonStyle(button,text,primary);return button;}
    static void ApplyButtonStyle(Button button,string text,bool primary)
    {
        button.Content=text;button.Background=Brush(primary?"#4263EB":"#EDF2FF");button.Foreground=Brush(primary?"#FFFFFF":"#3658D4");button.BorderThickness=new Thickness(0);
        button.Padding=new Thickness(21,11,21,11);button.FontWeight=FontWeights.SemiBold;button.Cursor=Cursors.Hand;
        var template=new ControlTemplate(typeof(Button));var border=new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty,new CornerRadius(10));
        border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
        var content=new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
        border.AppendChild(content);template.VisualTree=border;
        var hover=new Trigger{Property=IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(OpacityProperty,0.88));template.Triggers.Add(hover);
        var disabled=new Trigger{Property=IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(OpacityProperty,0.46));template.Triggers.Add(disabled);
        button.Template=template;
    }
    DataTemplate CreateRowTemplate()
    {
        var template=new DataTemplate();var row=new FrameworkElementFactory(typeof(DockPanel));
        row.SetValue(FrameworkElement.MarginProperty,new Thickness(7,5,7,5));
        var remove=new FrameworkElementFactory(typeof(Button));remove.SetValue(ContentControl.ContentProperty,"×");remove.SetValue(DockPanel.DockProperty,Dock.Right);
        remove.SetValue(FrameworkElement.ToolTipProperty,"Odstrániť dokument");remove.SetValue(Control.PaddingProperty,new Thickness(8,2,8,2));
        remove.SetValue(FrameworkElement.MarginProperty,new Thickness(8,0,0,0));
        remove.AddHandler(Button.ClickEvent,new RoutedEventHandler((s,e)=>{e.Handled=true;if(busy||s is not FrameworkElement el||el.DataContext is not string path)return;documents.Remove(path);UpdateCount();}));row.AppendChild(remove);
        var filename=new FrameworkElementFactory(typeof(TextBlock));
        filename.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding{Converter=new FileNameConverter()});
        filename.SetBinding(FrameworkElement.ToolTipProperty,new System.Windows.Data.Binding());
        filename.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);
        filename.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
        row.AppendChild(filename);template.VisualTree=row;return template;
    }
    void AnimateDrop(bool active)
    {
        dropBackground.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(active?Color.FromRgb(237,242,255):Colors.White,TimeSpan.FromMilliseconds(170)));
        dropBorder.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(active?Color.FromRgb(66,99,235):Color.FromRgb(217,225,239),TimeSpan.FromMilliseconds(170)));
        dropTitle.Text=active?"Pustite dokumenty sem":"Presuňte sem dokumenty";
    }
    void OnDragOver(object sender,DragEventArgs e)
    {
        bool external=!busy&&e.Data.GetDataPresent(DataFormats.FileDrop);
        bool internalDrag=!busy&&e.Data.GetDataPresent("DocxMerge.Row");
        e.Effects=external?DragDropEffects.Copy:internalDrag?DragDropEffects.Move:DragDropEffects.None;
        if(external&&dropTitle.Text!="Pustite dokumenty sem")AnimateDrop(true);
        e.Handled=true;
    }
    void OnDrop(object sender,DragEventArgs e)
    {
        AnimateDrop(false);
        if(busy){e.Handled=true;return;}
        if(e.Data.GetData(DataFormats.FileDrop) is string[] paths)AddFiles(paths);
        else if(e.Data.GetData("DocxMerge.Row") is string path)
        {
            var source=e.OriginalSource as DependencyObject;
            var item=source is null?null:ItemsControl.ContainerFromElement(list,source) as ListBoxItem;
            int from=documents.IndexOf(path),to=item is null?documents.Count-1:list.ItemContainerGenerator.IndexFromContainer(item);
            if(from>=0&&to>=0&&from!=to)documents.Move(from,to);
            UpdateCount();
        }
        e.Handled=true;
    }
    static bool InButton(DependencyObject element)
    {
        for(DependencyObject? node=element;node!=null;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node))
            if(node is Button)return true;
        return false;
    }
    void OnMouseDown(object sender,MouseButtonEventArgs e)
    {
        dragStart=e.GetPosition(list);dragged=null;
        var source=e.OriginalSource as DependencyObject;
        if(source is null||InButton(source))return;
        dragged=(ItemsControl.ContainerFromElement(list,source) as ListBoxItem)?.DataContext as string;
    }
    void OnMouseMove(object sender,MouseEventArgs e)
    {
        if(busy||dragged is null||e.LeftButton!=MouseButtonState.Pressed)return;
        var difference=e.GetPosition(list)-dragStart;
        if(Math.Abs(difference.X)<SystemParameters.MinimumHorizontalDragDistance&&Math.Abs(difference.Y)<SystemParameters.MinimumVerticalDragDistance)return;
        string path=dragged;dragged=null;
        DragDrop.DoDragDrop(list,new DataObject("DocxMerge.Row",path),DragDropEffects.Move);
    }
    void Browse()
    {
        var dialog=new OpenFileDialog{Title="Vyberte dokumenty",Filter="Dokumenty Word (*.docx)|*.docx",Multiselect=true};
        if(dialog.ShowDialog(this)==true)AddFiles(dialog.FileNames);
    }
    void AddFiles(IEnumerable<string> paths)
    {
        int skipped=0;
        foreach(string path in paths)
        {
            if(!File.Exists(path)||!Path.GetExtension(path).Equals(".docx",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(path).StartsWith("~$")){skipped++;continue;}
            string full=Path.GetFullPath(path);
            if(!documents.Any(x=>x.Equals(full,StringComparison.OrdinalIgnoreCase)))documents.Add(full);
        }
        UpdateCount();
        if(skipped>0)status.Text+=" Nepodporované súbory boli preskočené.";
    }
    void UpdateCount()
    {
        merge.IsEnabled=documents.Count>=2&&!busy;
        status.Text=documents.Count<2?"Pridajte aspoň dva dokumenty DOCX.":$"Počet dokumentov: {documents.Count} • Poradie zhora nadol";
    }
    async Task MergeAsync()
    {
        if(busy||documents.Count<2)return;
        var dialog=new SaveFileDialog{Title="Uložiť zlúčený dokument",Filter="Dokument Word (*.docx)|*.docx",DefaultExt=".docx",AddExtension=true,FileName="Zlúčené dokumenty.docx",OverwritePrompt=true};
        if(dialog.ShowDialog(this)!=true)return;
        string destination=Path.GetFullPath(dialog.FileName);
        if(documents.Any(x=>x.Equals(destination,StringComparison.OrdinalIgnoreCase))){status.Text="Vyberte iný názov. Pôvodný dokument nemožno prepísať.";return;}
        var inputs=documents.ToArray();busy=true;layout.IsEnabled=false;status.Text="Zlučovanie… chvíľu počkajte.";
        try
        {
            await Task.Run(()=>{
                var merged=DocumentBuilder.BuildDocument(inputs.Select(x=>new Source(new WmlDocument(x),true)).ToList());
                string temp=Path.Combine(Path.GetDirectoryName(destination)!,".docxmerge-"+Guid.NewGuid()+".docx");
                try{merged.SaveAs(temp);if(File.Exists(destination))File.Replace(temp,destination,null);else File.Move(temp,destination);}
                finally{if(File.Exists(temp))File.Delete(temp);}
            });
            status.Text="Hotovo. Dokument bol uložený.";
            MessageBox.Show(this,"Dokument bol uložený:\n\n"+destination,"Hotovo",MessageBoxButton.OK,MessageBoxImage.Information);
        }
        catch(Exception ex)
        {
            status.Text="Dokumenty sa nepodarilo zlúčiť.";
            MessageBox.Show(this,"Skontrolujte dokumenty a cieľový priečinok.\n\n"+ex.Message,"Zlučovanie zlyhalo",MessageBoxButton.OK,MessageBoxImage.Warning);
        }
        finally{busy=false;layout.IsEnabled=true;merge.IsEnabled=documents.Count>=2;}
    }
}
public sealed class FileNameConverter:System.Windows.Data.IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>Path.GetFileName(value as string??"");
    public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();
}
