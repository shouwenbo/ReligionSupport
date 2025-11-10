using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KoreanSoftKeyboard.WPF
{
    public class KeyButton : Button
    {
        private readonly KeyModel _model;


        public bool IsPressed
        {
            get => (bool)GetValue(IsPressedProperty);
            set => SetValue(IsPressedProperty, value);
        }


        public static readonly DependencyProperty IsPressedProperty = DependencyProperty.Register("IsPressed", typeof(bool), typeof(KeyButton), new PropertyMetadata(false, OnPressedChanged));


        private static void OnPressedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kb = d as KeyButton;
            kb.UpdateVisualState();
        }


        public string OutputChar => _model.Korean;


        public KeyButton(KeyModel model)
        {
            _model = model;
            this.Width = model.IsWide ? 90 : 56;
            this.Height = 56;
            this.Padding = new Thickness(4);
            this.FontFamily = new FontFamily("Segoe UI");
            this.Template = CreateControlTemplate();
            this.Cursor = Cursors.Hand;
            UpdateVisualState();
        }



        private ControlTemplate CreateControlTemplate()
        {
            var xaml = @"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='Button'>
    <Grid>
        <Border x:Name='Bd' CornerRadius='2' 
                Background='{TemplateBinding Background}' 
                BorderThickness='1'>
            <Border.BorderBrush>
                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                    <GradientStop Color='#FFFFFF' Offset='0'/>
                    <GradientStop Color='#A0A0A0' Offset='1'/>
                </LinearGradientBrush>
            </Border.BorderBrush>
            <Grid Margin='4'>
                <Grid.RowDefinitions>
                    <RowDefinition Height='Auto'/>
                    <RowDefinition Height='*'/>
                    <RowDefinition Height='Auto'/>
                </Grid.RowDefinitions>
                <TextBlock Text='{Binding SmallText}' FontSize='9' 
                           HorizontalAlignment='Left' VerticalAlignment='Top'
                           Foreground='#505050'/>
                <TextBlock Text='{Binding BigText}' FontSize='16'
                           Grid.Row='1' HorizontalAlignment='Center'
                           VerticalAlignment='Center' FontWeight='Bold'/>
                <TextBlock Text='{Binding ShiftText}' FontSize='10'
                           Grid.Row='2' HorizontalAlignment='Right'
                           VerticalAlignment='Bottom' Foreground='#808080'/>
            </Grid>
        </Border>
    </Grid>
    <ControlTemplate.Triggers>
        <Trigger Property='IsPressed' Value='True'>
            <Setter TargetName='Bd' Property='Background' Value='#CFE2FF'/>
            <Setter TargetName='Bd' Property='BorderBrush'>
                <Setter.Value>
                    <SolidColorBrush Color='#2F6DB1'/>
                </Setter.Value>
            </Setter>
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>";
            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
        }





        private void UpdateVisualState()
        {
            if (IsPressed)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(39, 109, 214));
                this.Foreground = Brushes.White;
            }
            else
            {
                this.Background = Brushes.Transparent;
                this.Foreground = Brushes.Black;
            }

            // Set DataContext for template bindings
            this.DataContext = new
            {
                SmallText = _model.English,
                BigText = _model.Korean,
                ShiftText = _model.KoreanShift
            };

        }
    }
}