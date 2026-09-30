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
    readonly ObservableCollection<string> files=new();
    readonly Grid root=new(); readonly ListBox list=new(); readonly TextBlock status=new(),dropLabel=new(); readonly Button merge=new();
    readonly Border drop=new(); readonly SolidColorBrush dropFill=new(Color.FromRgb(255,255,255)),dropStroke=new(Color.FromRgb(217,225,239));
    Point start; string? dragging; bool busy;
    static Brush Ink(string hex)=>(Brush)new BrushConverter().ConvertFromString(hex)!;
    public MainWindow()
    {
        Title="Merge Documents";Width=640;Height=670;MinWidth=520;MinHeight=530;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Background=Ink("#F5F7FB");Foreground=Ink("#172033");FontFamily=new FontFamily("Segoe UI");FontSize=14;
        root.Margin=new Thickness(30);Content=root;
        foreach(var height in new[]{GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto})root.RowDefinitions.Add(new RowDefinition{Height=height});
        var heading=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,24)};
        try {var image=new BitmapImage(new Uri("pack://application:,,,/logo.png"));Icon=image;heading.Children.Add(new Image{Source=image,Width=52,Height=52,Stretch=Stretch.Uniform,Margin=new Thickness(0,0,16,0)});}catch{}
        var titles=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
        titles.Children.Add(new TextBlock{Text="Merge documents",FontSize=27,FontWeight=FontWeights.SemiBold});
        titles.Children.Add(new TextBlock{Text="Multiple documents. One file.",Foreground=Ink("#667085"),Margin=new Thickness(0,6,0,0)});
        heading.Children.Add(titles);root.Children.Add(heading);
        drop.Background=dropFill;drop.BorderBrush=dropStroke;drop.BorderThickness=new Thickness(2);drop.CornerRadius=new CornerRadius(18);drop.Padding=new Thickness(22);drop.Margin=new Thickness(0,0,0,18);
        var prompt=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center};
        prompt.Children.Add(new TextBlock{Text="DOCX",FontWeight=FontWeights.Bold,Foreground=Ink("#4263EB"),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,12)});
        dropLabel.Text="Drop your documents here";dropLabel.FontSize=20;dropLabel.FontWeight=FontWeights.SemiBold;dropLabel.TextAlignment=TextAlignment.Center;prompt.Children.Add(dropLabel);
        prompt.Children.Add(new TextBlock{Text="or select them from your computer",Foreground=Ink("#667085"),TextAlignment=TextAlignment.Center,Margin=new Thickness(0,7,0,16)});
        var add=MakeButton("Add documents",false);add.HorizontalAlignment=HorizontalAlignment.Center;add.Click+=(_,_)=>Browse();prompt.Children.Add(add);
        drop.Child=prompt;Grid.SetRow(drop,1);root.Children.Add(drop);
        list.ItemsSource=files;list.Background=Ink("#FFFFFF");list.BorderBrush=Ink("#E5EAF4");list.BorderThickness=new Thickness(1);list.Padding=new Thickness(8);list.HorizontalContentAlignment=HorizontalAlignment.Stretch;
        list.ItemTemplate=Rows();Grid.SetRow(list,2);root.Children.Add(list);
        var footer=new StackPanel{Margin=new Thickness(0,16,0,0)};
        status.Foreground=Ink("#667085");status.TextAlignment=TextAlignment.Center;status.TextWrapping=TextWrapping.Wrap;status.Margin=new Thickness(0,0,0,12);footer.Children.Add(status);
        StyleButton(merge,"Merge and save",true);merge.Height=49;merge.Click+=async(_,_)=>await MergeAsync();footer.Children.Add(merge);
        footer.Children.Add(new TextBlock{Text="Your original documents will not be changed.",Foreground=Ink("#98A2B3"),FontSize=12,TextAlignment=TextAlignment.Center,Margin=new Thickness(0,12,0,0)});
        footer.Children.Add(new TextBlock{Text="© 2026 Seb Matt www.krabicahub.xyz",Foreground=Ink("#98A2B3"),FontSize=11,TextAlignment=TextAlignment.Center,Margin=new Thickness(0,15,0,0)});
        Grid.SetRow(footer,3);root.Children.Add(footer);
        AllowDrop=true;
        PreviewDragOver+=(_,e)=>{
            bool external=!busy&&e.Data.GetDataPresent(DataFormats.FileDrop);
            bool internalMove=!busy&&e.Data.GetDataPresent("DocxMerge.Row");
            e.Effects=external?DragDropEffects.Copy:internalMove?DragDropEffects.Move:DragDropEffects.None;
            if(external&&dropLabel.Text!="Release to add documents")Highlight(true);
            e.Handled=true;
        };
        DragLeave+=(_,_)=>{if(!IsMouseOver)Highlight(false);};
        PreviewDrop+=(_,e)=>{
            Highlight(false);
            if(!busy && e.Data.GetData(DataFormats.FileDrop) is string[] paths)AddFiles(paths);
            else if(!busy && e.Data.GetData("DocxMerge.Row") is string path){
                var origin=e.OriginalSource as DependencyObject;
                var item=origin is null?null:ItemsControl.ContainerFromElement(list,origin) as ListBoxItem;
                int before=files.IndexOf(path),after=item is null?files.Count-1:list.ItemContainerGenerator.IndexFromContainer(item);
                if(before>=0&&after>=0&&before!=after)files.Move(before,after);
                UpdateStatus();
            }
            e.Handled=true;
        };
        list.PreviewMouseLeftButtonDown+=(_,e)=>{
            start=e.GetPosition(list);dragging=null;var node=e.OriginalSource as DependencyObject;
            if(node is null||InsideButton(node))return;
            dragging=(ItemsControl.ContainerFromElement(list,node) as ListBoxItem)?.DataContext as string;
        };
        list.PreviewMouseMove+=(_,e)=>{
            if(busy||dragging is null||e.LeftButton!=MouseButtonState.Pressed)return;
            var delta=e.GetPosition(list)-start;
            if(Math.Abs(delta.X)<SystemParameters.MinimumHorizontalDragDistance&&Math.Abs(delta.Y)<SystemParameters.MinimumVerticalDragDistance)return;
            string path=dragging;dragging=null;
            DragDrop.DoDragDrop(list,new DataObject("DocxMerge.Row",path),DragDropEffects.Move);
            Highlight(false);
        };
        Closing+=(_,e)=>{if(busy)e.Cancel=true;};
        Loaded+=(_,_)=>{root.Opacity=0;root.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(280)));};
        UpdateStatus();
    }
    static bool InsideButton(DependencyObject node)
    {
        for(DependencyObject? current=node;current!=null;current=current is Visual?VisualTreeHelper.GetParent(current):LogicalTreeHelper.GetParent(current))if(current is Button)return true;
        return false;
    }
    static Button MakeButton(string label,bool primary){var b=new Button();StyleButton(b,label,primary);return b;}
    static void StyleButton(Button button,string label,bool primary)
    {
        button.Content=label;button.Background=Ink(primary?"#4263EB":"#EDF2FF");button.Foreground=Ink(primary?"#FFFFFF":"#3658D4");
        button.Padding=new Thickness(21,11,21,11);button.BorderThickness=new Thickness(0);button.FontWeight=FontWeights.SemiBold;button.Cursor=Cursors.Hand;
        var template=new ControlTemplate(typeof(Button));var border=new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty,new CornerRadius(10));
        border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
        var presenter=new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
        border.AppendChild(presenter);template.VisualTree=border;
        var hover=new Trigger{Property=IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(OpacityProperty,0.87));template.Triggers.Add(hover);
        var off=new Trigger{Property=IsEnabledProperty,Value=false};off.Setters.Add(new Setter(OpacityProperty,0.45));template.Triggers.Add(off);
        button.Template=template;
    }
    DataTemplate Rows()
    {
        var template=new DataTemplate();var row=new FrameworkElementFactory(typeof(DockPanel));row.SetValue(FrameworkElement.MarginProperty,new Thickness(8,5,8,5));
        var remove=new FrameworkElementFactory(typeof(Button));remove.SetValue(ContentControl.ContentProperty,"×");remove.SetValue(DockPanel.DockProperty,Dock.Right);
        remove.SetValue(FrameworkElement.ToolTipProperty,"Remove document");remove.SetValue(Control.PaddingProperty,new Thickness(8,2,8,2));remove.SetValue(FrameworkElement.MarginProperty,new Thickness(8,0,0,0));
        remove.AddHandler(Button.ClickEvent,new RoutedEventHandler((sender,e)=>{e.Handled=true;if(!busy&&sender is FrameworkElement el&&el.DataContext is string path){files.Remove(path);UpdateStatus();}}));row.AppendChild(remove);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding{Converter=new FileNameConverter()});text.SetBinding(FrameworkElement.ToolTipProperty,new System.Windows.Data.Binding());
        text.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);text.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);row.AppendChild(text);
        template.VisualTree=row;return template;
    }
    void Highlight(bool active)
    {
        dropFill.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(active?Color.FromRgb(237,242,255):Colors.White,TimeSpan.FromMilliseconds(170)));
        dropStroke.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(active?Color.FromRgb(66,99,235):Color.FromRgb(217,225,239),TimeSpan.FromMilliseconds(170)));
        dropLabel.Text=active?"Release to add documents":"Drop your documents here";
    }
    void Browse(){var dialog=new OpenFileDialog{Title="Select documents",Filter="Word documents (*.docx)|*.docx",Multiselect=true};if(dialog.ShowDialog(this)==true)AddFiles(dialog.FileNames);}
    void AddFiles(IEnumerable<string> paths)
    {
        int skipped=0;
        foreach(var path in paths){
            if(!File.Exists(path)||!Path.GetExtension(path).Equals(".docx",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(path).StartsWith("~$")){skipped++;continue;}
            string full=Path.GetFullPath(path);if(!files.Any(x=>x.Equals(full,StringComparison.OrdinalIgnoreCase)))files.Add(full);
        }
        UpdateStatus();if(skipped>0)status.Text+=" Unsupported files were skipped.";
    }
    void UpdateStatus(){merge.IsEnabled=files.Count>=2&&!busy;status.Text=files.Count<2?"Add at least two DOCX documents.":$"{files.Count} documents • Drag a name to change the order";}
    async Task MergeAsync()
    {
        if(busy||files.Count<2)return;
        var dialog=new SaveFileDialog{Title="Save merged document",Filter="Word document (*.docx)|*.docx",DefaultExt=".docx",AddExtension=true,FileName="Merged documents.docx",OverwritePrompt=true};
        if(dialog.ShowDialog(this)!=true)return;
        string destination=Path.GetFullPath(dialog.FileName);
        if(files.Any(x=>x.Equals(destination,StringComparison.OrdinalIgnoreCase))){status.Text="Choose another filename. An original document cannot be overwritten.";return;}
        var inputs=files.ToArray();busy=true;root.IsEnabled=false;status.Text="Merging... Please wait.";
        try{
            await Task.Run(()=>{
                var result=DocumentBuilder.BuildDocument(inputs.Select(x=>new Source(new WmlDocument(x),true)).ToList());
                string temp=Path.Combine(Path.GetDirectoryName(destination)!,".docxmerge-"+Guid.NewGuid()+".docx");
                try{result.SaveAs(temp);if(File.Exists(destination))File.Replace(temp,destination,null);else File.Move(temp,destination);}
                finally{if(File.Exists(temp))File.Delete(temp);}
            });
            status.Text="Done! Your document has been saved.";
            MessageBox.Show(this,"Your document was saved:\n\n"+destination,"Done",MessageBoxButton.OK,MessageBoxImage.Information);
        }catch(Exception ex){
            status.Text="The documents could not be merged.";
            MessageBox.Show(this,"Check your documents and save location.\n\n"+ex.Message,"Merge failed",MessageBoxButton.OK,MessageBoxImage.Warning);
        }finally{busy=false;root.IsEnabled=true;merge.IsEnabled=files.Count>=2;}
    }
}
public sealed class FileNameConverter:System.Windows.Data.IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>Path.GetFileName(value as string??"");
    public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();
}
