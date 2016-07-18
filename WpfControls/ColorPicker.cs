using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;   //For the Selector reference.
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfControls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfControls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfControls;assembly=WpfControls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:ColorPicker/>
    ///
    /// </summary>
    public class ColorPicker : Control
    {
        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        public ColorPicker()
        {
            PaletteStandard.Add(Colors.Black);
            PaletteStandard.Add(Colors.White);
            PaletteStandard.Add(Colors.Red);
            PaletteStandard.Add(Colors.OrangeRed);
            PaletteStandard.Add(Colors.Orange);            
            PaletteStandard.Add(Colors.Yellow);
            PaletteStandard.Add(Colors.YellowGreen);
            PaletteStandard.Add(Colors.Green);            
            PaletteStandard.Add(Colors.Blue);
            PaletteStandard.Add(Colors.Indigo);
            PaletteStandard.Add(Colors.Purple);

            SelectedColor = PaletteStandard[0];

            PaletteRecent.Add(SelectedColor);

            PaletteAdvanced.Add(Colors.Red);

            PaletteRecent.CollectionChanged += Colors_CollectionChanged;
            PaletteStandard.CollectionChanged += Colors_CollectionChanged;
            PaletteAdvanced.CollectionChanged += Colors_CollectionChanged;



        }

        protected Selector Selector { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Selector = GetTemplateChild("PART_Selector") as Selector;
            if (Selector == null) throw new InvalidOperationException("Objects of type ColorPicker must contain a visual member entitled \"PART_Selector\" which binds to the SelectedColor.");
            Selector.SelectionChanged += Selector_SelectionChanged;
            Selector.SelectedValue = SelectedColor;
            //Console.WriteLine(Selector.SelectedValue == null ? "Null" : Selector.SelectedValue.ToString());
            UpdatePalettes();
        }

     

        #region ColorPicker dependency properties

        public static DependencyProperty SelectedColorProperty = 
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPicker), new PropertyMetadata(Colors.Black));
        public Color SelectedColor
        {
            get
            {
                return (Color)GetValue(SelectedColorProperty);
            }
            set
            {
                SetValue(SelectedColorProperty, value);
            }
        }
        

        public static DependencyProperty ShowStandardProperty =
            DependencyProperty.Register("ShowStandard", typeof(bool), typeof(ColorPicker), new PropertyMetadata(true, new PropertyChangedCallback(UpdatePalettes)));
        public bool ShowStandard
        {
            get
            {
                return (bool)GetValue(ShowStandardProperty);
            }
            set
            {
                SetValue(ShowStandardProperty, value);
            }
        }


        public static DependencyProperty ShowRecentProperty =
            DependencyProperty.Register("ShowRecent", typeof(bool), typeof(ColorPicker), new PropertyMetadata(true, new PropertyChangedCallback(UpdatePalettes)));
        public bool ShowRecent
        {
            get
            {
                return (bool)GetValue(ShowRecentProperty);
            }
            set
            {
                SetValue(ShowRecentProperty, value);
            }
        }


        public static DependencyProperty ShowAdvancedProperty =
            DependencyProperty.Register("ShowAdvanced", typeof(bool), typeof(ColorPicker), new PropertyMetadata(true, new PropertyChangedCallback(UpdatePalettes)));
        public bool ShowAdvanced
        {
            get
            {
                return (bool)GetValue(ShowAdvancedProperty);
            }
            set
            {
                SetValue(ShowAdvancedProperty, value);
            }
        }


        #endregion






        #region ColorPicker palettes

        private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource == Selector)
            {
                if (e.AddedItems[0] is Color) SelectedColor = (Color)e.AddedItems[0];
                else if (e.AddedItems[0] is CategoryColor) SelectedColor = ((CategoryColor)e.AddedItems[0]).Color;
                else throw new InvalidOperationException("Selector may only select different color structs.");                
            }
        }

        private static void UpdatePalettes(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((ColorPicker)obj).UpdatePalettes();
        }
        protected void UpdatePalettes()
        {
            List<CategoryColor> colors = new List<CategoryColor>();
            if (ShowStandard) foreach (Color c in PaletteStandard) colors.Add(new CategoryColor("Standard", c));
            if (ShowRecent) foreach (Color c in PaletteRecent) colors.Add(new CategoryColor("Recent", c));
            if (ShowAdvanced) foreach (Color c in PaletteAdvanced) colors.Add(new CategoryColor("Advanced", c));
            
            CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(colors);
            
            cv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            Selector.SelectedValuePath = "Color";
            Selector.ItemsSource = cv;          

        }

        public ObservableCollection<Color> PaletteStandard { get; } = new ObservableCollection<Color>();

        public ObservableCollection<Color> PaletteRecent { get; } = new ObservableCollection<Color>();

        public ObservableCollection<Color> PaletteAdvanced { get; } = new ObservableCollection<Color>();

        private void Colors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdatePalettes();
        }

        /// <summary>
        /// A lightweight data object used to group colors according to their category.
        /// </summary>
        private struct CategoryColor
        {
            public string Category { get; }
            public Color Color { get; }
            public CategoryColor(string category, Color color)
            {
                this.Category = category;
                this.Color = color;
            }
        }

        #endregion
    }
}
