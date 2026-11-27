using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Markup;

namespace x8086NetEmuEto.Wpf {
    // Dark styling for the native WPF chrome Eto doesn't expose (buttons, check boxes, combo boxes, grid
    // headers and rows, scroll bars, group boxes, tabs, menus, tool tips) plus dark window title bars.
    // Colors match x8086NetEmuEto.Theme.
    internal static class DarkTheme {
        public static void Install() {
            ResourceDictionary rd = (ResourceDictionary)XamlReader.Parse(Xaml);
            AddDerivedStyles(rd);
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(rd);

            // Every WPF window (Eto forms, dialogs, popups' owners) gets a dark title bar
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((s, e) => UseDarkTitleBar((Window)s)));
        }

        // Implicit styles only apply to the exact type, and Eto uses its own subclasses (EtoButton, EtoTextBox,
        // EtoDataGrid, ...). Give each one the base style; subclasses with their own default template (e.g. the
        // watermark text box) only get the colors so their template is kept.
        private static void AddDerivedStyles(ResourceDictionary rd) {
            Type[] styled = [typeof(Button), typeof(ToggleButton), typeof(CheckBox), typeof(TextBox), typeof(ComboBox), typeof(DataGrid),
                             typeof(GroupBox), typeof(TabControl), typeof(TabItem), typeof(ListBox), typeof(TreeView), typeof(ContextMenu), typeof(MenuItem)];

            FieldInfo dsk = typeof(FrameworkElement).GetField("DefaultStyleKeyProperty", BindingFlags.NonPublic | BindingFlags.Static);
            DependencyProperty defaultStyleKey = (DependencyProperty)dsk?.GetValue(null);

            IEnumerable<Type> etoTypes;
            try {
                etoTypes = typeof(Eto.Wpf.Platform).Assembly.GetTypes();
            } catch(ReflectionTypeLoadException ex) {
                etoTypes = ex.Types.Where(t => t != null);
            }

            foreach(Type t in etoTypes) {
                if(t.IsAbstract || t.IsGenericTypeDefinition || !typeof(FrameworkElement).IsAssignableFrom(t)) continue;
                if(t.Namespace?.StartsWith("Eto.Wpf.CustomControls") == true) continue; // e.g. the tree grid's expander arrows
                Type b = styled.Where(s => s != t && s.IsAssignableFrom(t)).OrderByDescending(Depth).FirstOrDefault();
                if(b == null || !rd.Contains(b) || rd.Contains(t)) continue;

                Style baseStyle = (Style)rd[b];
                // Text boxes with placeholder text draw it from their own template
                bool ownTemplate = (defaultStyleKey != null && Equals(defaultStyleKey.GetMetadata(t).DefaultValue, t)) ||
                                   t.Name.Contains("Watermark") || t.Name.Contains("Search");
                if(ownTemplate) {
                    Style colors = new(t);
                    foreach(SetterBase sb in baseStyle.Setters) {
                        if(sb is Setter s && s.Property != Control.TemplateProperty) colors.Setters.Add(new Setter(s.Property, s.Value));
                    }
                    rd[t] = colors;
                } else {
                    rd[t] = new Style(t, baseStyle);
                }
            }
        }

        private static int Depth(Type t) {
            int d = 0;
            for(Type b = t; b != null; b = b.BaseType) d++;
            return d;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private static void UseDarkTitleBar(Window window) {
            try {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if(hwnd == IntPtr.Zero) return;
                int on = 1;
                // DWMWA_USE_IMMERSIVE_DARK_MODE is 20 on Windows 10 20H1+ and 11, 19 on earlier Windows 10 builds
                if(DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)) != 0) DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int));
            } catch {
                // Older Windows: keep the default title bar
            }
        }

        private const string Xaml = """
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="Dk.Window" Color="#1F1F1F"/>
    <SolidColorBrush x:Key="Dk.Surface" Color="#181818"/>
    <SolidColorBrush x:Key="Dk.Input" Color="#2B2B2B"/>
    <SolidColorBrush x:Key="Dk.Button" Color="#333333"/>
    <SolidColorBrush x:Key="Dk.ButtonHover" Color="#3F3F3F"/>
    <SolidColorBrush x:Key="Dk.ButtonPressed" Color="#4C4C4C"/>
    <SolidColorBrush x:Key="Dk.Header" Color="#252526"/>
    <SolidColorBrush x:Key="Dk.Menu" Color="#2B2B2B"/>
    <SolidColorBrush x:Key="Dk.Border" Color="#454545"/>
    <SolidColorBrush x:Key="Dk.BorderStrong" Color="#6A6A6A"/>
    <SolidColorBrush x:Key="Dk.Text" Color="#E6E6E6"/>
    <SolidColorBrush x:Key="Dk.Dim" Color="#9A9A9A"/>
    <SolidColorBrush x:Key="Dk.Disabled" Color="#6E6E6E"/>
    <SolidColorBrush x:Key="Dk.Accent" Color="#3794FF"/>
    <SolidColorBrush x:Key="Dk.AccentFill" Color="#0E639C"/>
    <SolidColorBrush x:Key="Dk.Selection" Color="#264F78"/>
    <SolidColorBrush x:Key="Dk.Hover" Color="#2A2D2E"/>
    <SolidColorBrush x:Key="Dk.Thumb" Color="#4A4A4A"/>
    <SolidColorBrush x:Key="Dk.ThumbHover" Color="#6A6A6A"/>

    <!-- System colors used by the stock templates we don't replace (selection, tool tips, scroll corners...) -->
    <SolidColorBrush x:Key="{x:Static SystemColors.WindowBrushKey}" Color="#1F1F1F"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.WindowTextBrushKey}" Color="#E6E6E6"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.ControlBrushKey}" Color="#1F1F1F"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.ControlTextBrushKey}" Color="#E6E6E6"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.ControlLightBrushKey}" Color="#333333"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.ControlDarkBrushKey}" Color="#454545"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.GrayTextBrushKey}" Color="#6E6E6E"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.HighlightBrushKey}" Color="#264F78"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.HighlightTextBrushKey}" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.InactiveSelectionHighlightBrushKey}" Color="#37373D"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.InactiveSelectionHighlightTextBrushKey}" Color="#E6E6E6"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.MenuBrushKey}" Color="#2B2B2B"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.MenuTextBrushKey}" Color="#E6E6E6"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.InfoBrushKey}" Color="#2B2B2B"/>
    <SolidColorBrush x:Key="{x:Static SystemColors.InfoTextBrushKey}" Color="#E6E6E6"/>

    <!-- Buttons -->
    <Style x:Key="Dk.ButtonBase" TargetType="ButtonBase">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="Background" Value="{StaticResource Dk.Button}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Padding" Value="10,3"/>
        <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ButtonBase">
                    <Border x:Name="bd" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="4" SnapsToDevicePixels="True">
                        <ContentPresenter Margin="{TemplateBinding Padding}" RecognizesAccessKey="True"
                                          HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                          VerticalAlignment="{TemplateBinding VerticalContentAlignment}"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="{StaticResource Dk.ButtonHover}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="{StaticResource Dk.ButtonPressed}"/>
                        </Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="bd" Property="BorderBrush" Value="{StaticResource Dk.Accent}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Foreground" Value="{StaticResource Dk.Disabled}"/>
                            <Setter TargetName="bd" Property="Opacity" Value="0.6"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style TargetType="Button" BasedOn="{StaticResource Dk.ButtonBase}"/>
    <Style TargetType="ToggleButton" BasedOn="{StaticResource Dk.ButtonBase}"/>

    <!-- Check boxes -->
    <Style TargetType="CheckBox">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="Background" Value="{StaticResource Dk.Input}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.BorderStrong}"/>
        <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="CheckBox">
                    <Grid Background="Transparent" SnapsToDevicePixels="True">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Border x:Name="box" Width="15" Height="15" CornerRadius="3" VerticalAlignment="Center"
                                Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1">
                            <Path x:Name="check" Data="M 2.5,6.5 L 5.5,9.5 L 10.5,3.5" Stroke="White" StrokeThickness="1.8" Visibility="Collapsed"/>
                        </Border>
                        <ContentPresenter x:Name="content" Grid.Column="1" Margin="6,0,0,0" VerticalAlignment="Center" RecognizesAccessKey="True"/>
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsChecked" Value="True">
                            <Setter TargetName="check" Property="Visibility" Value="Visible"/>
                            <Setter TargetName="box" Property="Background" Value="{StaticResource Dk.AccentFill}"/>
                            <Setter TargetName="box" Property="BorderBrush" Value="{StaticResource Dk.AccentFill}"/>
                        </Trigger>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="box" Property="BorderBrush" Value="{StaticResource Dk.Accent}"/>
                        </Trigger>
                        <Trigger Property="Content" Value="{x:Null}">
                            <Setter TargetName="content" Property="Margin" Value="0"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.5"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Text boxes -->
    <Style TargetType="TextBox">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="Background" Value="{StaticResource Dk.Input}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CaretBrush" Value="{StaticResource Dk.Text}"/>
        <Setter Property="SelectionBrush" Value="{StaticResource Dk.Accent}"/>
        <Setter Property="Padding" Value="3,2"/>
        <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="TextBox">
                    <Border x:Name="bd" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="3" SnapsToDevicePixels="True">
                        <ScrollViewer x:Name="PART_ContentHost" Focusable="False" HorizontalScrollBarVisibility="{TemplateBinding HorizontalScrollBarVisibility}"
                                      VerticalScrollBarVisibility="{TemplateBinding VerticalScrollBarVisibility}"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="bd" Property="BorderBrush" Value="{StaticResource Dk.BorderStrong}"/>
                        </Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="bd" Property="BorderBrush" Value="{StaticResource Dk.Accent}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.5"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Combo boxes (Eto DropDown) -->
    <ControlTemplate x:Key="Dk.ComboToggle" TargetType="ToggleButton">
        <Border x:Name="bd" Background="{StaticResource Dk.Input}" BorderBrush="{StaticResource Dk.Border}" BorderThickness="1" CornerRadius="3">
            <Path HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,9,0" Data="M 0,0 L 4,4 L 8,0"
                  Stroke="{StaticResource Dk.Dim}" StrokeThickness="1.5"/>
        </Border>
        <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter TargetName="bd" Property="BorderBrush" Value="{StaticResource Dk.BorderStrong}"/>
            </Trigger>
            <Trigger Property="IsChecked" Value="True">
                <Setter TargetName="bd" Property="BorderBrush" Value="{StaticResource Dk.Accent}"/>
            </Trigger>
        </ControlTemplate.Triggers>
    </ControlTemplate>
    <Style TargetType="ComboBox">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ComboBox">
                    <Grid MinHeight="24">
                        <ToggleButton Template="{StaticResource Dk.ComboToggle}" Focusable="False" ClickMode="Press"
                                      IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}"/>
                        <ContentPresenter x:Name="selection" IsHitTestVisible="False" Margin="7,3,26,3" VerticalAlignment="Center" HorizontalAlignment="Left"
                                          Content="{TemplateBinding SelectionBoxItem}" ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                                          ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}"/>
                        <TextBox x:Name="PART_EditableTextBox" Visibility="Hidden" IsReadOnly="{TemplateBinding IsReadOnly}" Margin="1,1,24,1"
                                 Background="Transparent" BorderThickness="0"/>
                        <Popup x:Name="PART_Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True"
                               Focusable="False" PopupAnimation="Fade">
                            <Border MinWidth="{TemplateBinding ActualWidth}" MaxHeight="{TemplateBinding MaxDropDownHeight}" Margin="0,2,0,0"
                                    Background="{StaticResource Dk.Menu}" BorderBrush="{StaticResource Dk.Border}" BorderThickness="1" CornerRadius="3">
                                <ScrollViewer SnapsToDevicePixels="True">
                                    <StackPanel IsItemsHost="True" KeyboardNavigation.DirectionalNavigation="Contained"/>
                                </ScrollViewer>
                            </Border>
                        </Popup>
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsEditable" Value="True">
                            <Setter TargetName="PART_EditableTextBox" Property="Visibility" Value="Visible"/>
                            <Setter TargetName="selection" Property="Visibility" Value="Hidden"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.5"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style TargetType="ComboBoxItem">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="Padding" Value="7,3"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ComboBoxItem">
                    <Border x:Name="bd" Background="Transparent" Padding="{TemplateBinding Padding}">
                        <ContentPresenter/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsHighlighted" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="{StaticResource Dk.Selection}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Scroll bars -->
    <Style x:Key="Dk.ScrollPage" TargetType="RepeatButton">
        <Setter Property="Focusable" Value="False"/>
        <Setter Property="IsTabStop" Value="False"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="RepeatButton">
                    <Border Background="Transparent"/>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style x:Key="Dk.ScrollThumb" TargetType="Thumb">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Thumb">
                    <Border x:Name="bd" Background="{StaticResource Dk.Thumb}" CornerRadius="4"/>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="{StaticResource Dk.ThumbHover}"/>
                        </Trigger>
                        <Trigger Property="IsDragging" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="{StaticResource Dk.Dim}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style TargetType="ScrollBar">
        <Setter Property="Background" Value="{StaticResource Dk.Window}"/>
        <Setter Property="Width" Value="12"/>
        <Setter Property="MinWidth" Value="12"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ScrollBar">
                    <Border Background="{TemplateBinding Background}">
                        <Track x:Name="PART_Track" IsDirectionReversed="True">
                            <Track.DecreaseRepeatButton>
                                <RepeatButton Style="{StaticResource Dk.ScrollPage}" Command="ScrollBar.PageUpCommand"/>
                            </Track.DecreaseRepeatButton>
                            <Track.IncreaseRepeatButton>
                                <RepeatButton Style="{StaticResource Dk.ScrollPage}" Command="ScrollBar.PageDownCommand"/>
                            </Track.IncreaseRepeatButton>
                            <Track.Thumb>
                                <Thumb Style="{StaticResource Dk.ScrollThumb}" Margin="3,2"/>
                            </Track.Thumb>
                        </Track>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="Orientation" Value="Horizontal">
                <Setter Property="Width" Value="Auto"/>
                <Setter Property="MinWidth" Value="0"/>
                <Setter Property="Height" Value="12"/>
                <Setter Property="MinHeight" Value="12"/>
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="ScrollBar">
                            <Border Background="{TemplateBinding Background}">
                                <Track x:Name="PART_Track" IsDirectionReversed="False">
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Style="{StaticResource Dk.ScrollPage}" Command="ScrollBar.PageLeftCommand"/>
                                    </Track.DecreaseRepeatButton>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Style="{StaticResource Dk.ScrollPage}" Command="ScrollBar.PageRightCommand"/>
                                    </Track.IncreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb Style="{StaticResource Dk.ScrollThumb}" Margin="2,3"/>
                                    </Track.Thumb>
                                </Track>
                            </Border>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- Data grids (Eto GridView and TreeGridView) -->
    <Style TargetType="DataGrid">
        <Setter Property="Background" Value="{StaticResource Dk.Surface}"/>
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="RowBackground" Value="Transparent"/>
        <Setter Property="AlternatingRowBackground" Value="Transparent"/>
        <Setter Property="HorizontalGridLinesBrush" Value="Transparent"/>
        <Setter Property="VerticalGridLinesBrush" Value="Transparent"/>
        <Setter Property="HeadersVisibility" Value="Column"/>
    </Style>
    <Style TargetType="DataGridColumnHeader">
        <Setter Property="Foreground" Value="{StaticResource Dk.Dim}"/>
        <Setter Property="Background" Value="{StaticResource Dk.Header}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
        <Setter Property="BorderThickness" Value="0,0,1,1"/>
        <Setter Property="Padding" Value="6,4"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="DataGridColumnHeader">
                    <Grid>
                        <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center"/>
                        </Border>
                        <Thumb x:Name="PART_RightHeaderGripper" HorizontalAlignment="Right" Width="6" Cursor="SizeWE">
                            <Thumb.Template>
                                <ControlTemplate TargetType="Thumb">
                                    <Border Background="Transparent"/>
                                </ControlTemplate>
                            </Thumb.Template>
                        </Thumb>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style TargetType="DataGridRow">
        <Setter Property="Background" Value="Transparent"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="{StaticResource Dk.Hover}"/>
            </Trigger>
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="Background" Value="{StaticResource Dk.Selection}"/>
            </Trigger>
        </Style.Triggers>
    </Style>
    <Style TargetType="DataGridCell">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="DataGridCell">
                    <Border Background="{TemplateBinding Background}" SnapsToDevicePixels="True">
                        <ContentPresenter VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="Foreground" Value="White"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- Group boxes -->
    <Style TargetType="GroupBox">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="GroupBox">
                    <DockPanel>
                        <ContentPresenter DockPanel.Dock="Top" ContentSource="Header" Margin="2,0,0,4" RecognizesAccessKey="True"
                                          TextElement.Foreground="{StaticResource Dk.Dim}" TextElement.FontWeight="SemiBold"/>
                        <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1"
                                CornerRadius="4" Padding="{TemplateBinding Padding}">
                            <ContentPresenter/>
                        </Border>
                    </DockPanel>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Tabs -->
    <Style TargetType="TabControl">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="TabControl">
                    <DockPanel>
                        <TabPanel DockPanel.Dock="Top" IsItemsHost="True" Margin="0,0,0,4"/>
                        <Border Background="{TemplateBinding Background}">
                            <ContentPresenter ContentSource="SelectedContent"/>
                        </Border>
                    </DockPanel>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style TargetType="TabItem">
        <Setter Property="Foreground" Value="{StaticResource Dk.Dim}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="TabItem">
                    <Border x:Name="bd" Background="Transparent" BorderBrush="Transparent" BorderThickness="0,0,0,2" Padding="12,4" Margin="0,0,2,0">
                        <ContentPresenter ContentSource="Header" RecognizesAccessKey="True"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
                        </Trigger>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
                            <Setter TargetName="bd" Property="BorderBrush" Value="{StaticResource Dk.Accent}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Lists and trees -->
    <Style TargetType="ListBox">
        <Setter Property="Background" Value="{StaticResource Dk.Surface}"/>
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
    </Style>
    <Style TargetType="TreeView">
        <Setter Property="Background" Value="{StaticResource Dk.Surface}"/>
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
    </Style>

    <!-- Menus -->
    <Style TargetType="Menu">
        <Setter Property="Background" Value="{StaticResource Dk.Window}"/>
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
    </Style>
    <Style TargetType="ContextMenu">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="Background" Value="{StaticResource Dk.Menu}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ContextMenu">
                    <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" Padding="3" CornerRadius="4">
                        <StackPanel IsItemsHost="True" KeyboardNavigation.DirectionalNavigation="Cycle"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style x:Key="{x:Static MenuItem.SeparatorStyleKey}" TargetType="Separator">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Separator">
                    <Border Height="1" Margin="8,4" Background="{StaticResource Dk.Border}"/>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style TargetType="MenuItem">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="MenuItem">
                    <Border x:Name="bd" Background="Transparent" CornerRadius="3" SnapsToDevicePixels="True">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition x:Name="iconColumn" Width="28"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition x:Name="arrowColumn" Width="20"/>
                            </Grid.ColumnDefinitions>
                            <ContentPresenter x:Name="icon" ContentSource="Icon" Width="16" Height="16" VerticalAlignment="Center" HorizontalAlignment="Center"/>
                            <Path x:Name="check" Data="M 0,5 L 3.5,8.5 L 10,1.5" Stroke="{StaticResource Dk.Text}" StrokeThickness="1.6"
                                  VerticalAlignment="Center" HorizontalAlignment="Center" Visibility="Collapsed"/>
                            <ContentPresenter x:Name="header" Grid.Column="1" ContentSource="Header" RecognizesAccessKey="True" VerticalAlignment="Center" Margin="2,5,16,5"/>
                            <TextBlock x:Name="gesture" Grid.Column="2" Text="{TemplateBinding InputGestureText}" Foreground="{StaticResource Dk.Dim}"
                                       VerticalAlignment="Center" Margin="0,0,6,0"/>
                            <Path x:Name="arrow" Grid.Column="3" Data="M 0,0 L 4,4 L 0,8 Z" Fill="{StaticResource Dk.Dim}"
                                  VerticalAlignment="Center" HorizontalAlignment="Center" Visibility="Collapsed"/>
                            <Popup x:Name="PART_Popup" Placement="Right" HorizontalOffset="2" VerticalOffset="-4" AllowsTransparency="True" Focusable="False"
                                   PopupAnimation="Fade" IsOpen="{Binding IsSubmenuOpen, RelativeSource={RelativeSource TemplatedParent}}">
                                <Border Background="{StaticResource Dk.Menu}" BorderBrush="{StaticResource Dk.Border}" BorderThickness="1" Padding="3" CornerRadius="4">
                                    <StackPanel IsItemsHost="True" KeyboardNavigation.DirectionalNavigation="Cycle"/>
                                </Border>
                            </Popup>
                        </Grid>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="Role" Value="SubmenuHeader">
                            <Setter TargetName="arrow" Property="Visibility" Value="Visible"/>
                        </Trigger>
                        <Trigger Property="Role" Value="TopLevelHeader">
                            <Setter TargetName="iconColumn" Property="Width" Value="0"/>
                            <Setter TargetName="arrowColumn" Property="Width" Value="0"/>
                            <Setter TargetName="header" Property="Margin" Value="8,4"/>
                            <Setter TargetName="PART_Popup" Property="Placement" Value="Bottom"/>
                            <Setter TargetName="PART_Popup" Property="HorizontalOffset" Value="0"/>
                            <Setter TargetName="PART_Popup" Property="VerticalOffset" Value="1"/>
                        </Trigger>
                        <Trigger Property="Role" Value="TopLevelItem">
                            <Setter TargetName="iconColumn" Property="Width" Value="0"/>
                            <Setter TargetName="arrowColumn" Property="Width" Value="0"/>
                            <Setter TargetName="header" Property="Margin" Value="8,4"/>
                        </Trigger>
                        <Trigger Property="IsChecked" Value="True">
                            <Setter TargetName="check" Property="Visibility" Value="Visible"/>
                        </Trigger>
                        <Trigger Property="Icon" Value="{x:Null}">
                            <Setter TargetName="icon" Property="Visibility" Value="Collapsed"/>
                        </Trigger>
                        <Trigger Property="IsHighlighted" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="{StaticResource Dk.Selection}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Foreground" Value="{StaticResource Dk.Disabled}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType="ToolTip">
        <Setter Property="Foreground" Value="{StaticResource Dk.Text}"/>
        <Setter Property="Background" Value="{StaticResource Dk.Menu}"/>
        <Setter Property="BorderBrush" Value="{StaticResource Dk.Border}"/>
    </Style>
</ResourceDictionary>
""";
    }
}
