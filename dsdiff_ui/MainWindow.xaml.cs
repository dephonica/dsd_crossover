using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace dsdiff_cross_ui_wpf
{
    public partial class MainWindow
    {
        private FilterEdit.FilterType _lastFilterType = FilterEdit.FilterType.LowPass;
        private string _sourceFile, _targetFile, _configFile;

        private Process _runningProcess = null;
        private EventWaitHandle _terminateEvent;
        private string _runningAnimation = ".";
        private int _animationCounter = 0;

        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
        }

        private void WindowLoaded1(object sender, RoutedEventArgs e)
        {
            Maximize();

            Image1.Opacity = 0;

            Image2.Opacity = 0;
            Image2.IsEnabled = false;

            Image3.Opacity = 0;

            ImageProcessingInfo.Opacity = 0;

            Grid2.Opacity = 0;

            Grid3.Opacity = 0;

//            Grid2.RenderTransform = new TranslateTransform(0, -Grid2.ActualHeight - 10);
//            Grid3.RenderTransform = new TranslateTransform(0, -Grid3.ActualHeight - 10);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            CloseRunningProcess();
        }

        private void Maximize()
        {
            MyAnimations.AnimateOpacity(this, 0.1, 1, 300);
            MyAnimations.AnimateRenderScale(this, 0.1, 1, ActualWidth / 2, ActualHeight / 2, 300);
        }

        private void GridMouseEnter1(object sender, MouseEventArgs e)
        {
            try
            {
                var element = (UIElement) sender;
                if (element.IsEnabled)
                {
                    if (element.Opacity < 0.001) return;
                    MyAnimations.AnimateOpacity(element, element.Opacity, 1.0, 100);
                    //element.Opacity = 1.0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void GridMouseLeave1(object sender, MouseEventArgs e)
        {
            try
            {
                var element = (UIElement) sender;
                if (element.Opacity < 0.001) return;
                MyAnimations.AnimateOpacity(element, element.Opacity, 0.7, 100);
                //element.Opacity = 0.7;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SelectSourceMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Grid1.IsEnabled == false) return;

            var grid = (Grid)sender;
            MyAnimations.AnimateRenderScale(grid, 1.0, 0.98,
                grid.ActualWidth / 2, grid.ActualHeight / 2, 100, (o, args) =>
                    {
                        var fileName = "";
                        if (OpenFileDialog(ref fileName) == false) return;

                        _sourceFile = fileName;

                        var runtimeWrap = new RuntimeWrap();

                        try
                        {
                            var fileInfo = new Dictionary<string, string>();
                            var response = runtimeWrap.GetFileResponse(fileName, ref fileInfo);

                            var fileFreq = long.Parse(fileInfo["samplerate"]);
                            var fileSamples = long.Parse(fileInfo["samples_per_channel"]);
                            var duration = fileSamples*8/fileFreq;

                            DurationLabel.Content = "File duration: " + TimeSpan.FromSeconds(duration).ToString();

                            var responseConvert =
                                response.Select(
                                    tuple => new PointF((float) tuple.Item1, (float) Math.Log(tuple.Item2)))
                                        .ToArray();

                            SourceGraph.SetPoints(0, responseConvert, Colors.LightSteelBlue, "Source");

                            if (Grid2.Opacity < 1.0)
                            {
                                Dispatcher.InvokeAsync(() =>
                                    {
                                        MyAnimations.AnimateRenderTranslation(Grid2, TranslateTransform.YProperty,
                                                                              -Grid1.ActualHeight - 20, 0, 300, 50);
                                        MyAnimations.AnimateOpacity(Grid2, 0, 0.7, 100, 50);
                                        MyAnimations.AnimateOpacity(LabelSelectDsdiff, 1.0, 0.0, 300);
                                        MyAnimations.AnimateOpacity(Image1, 0.0, 1.0, 300);

                                        if (LabelLoadError.Opacity > 0)
                                            MyAnimations.AnimateOpacity(LabelLoadError, 1.0, 0, 300);
                                    });
                            }

                            if (LabelWriteOutput.Opacity < 1.0)
                            {
                                Dispatcher.InvokeAsync(() =>
                                    {
                                        MyAnimations.AnimateOpacity(LabelWriteOutput, 0, 1.0, 300);
                                        MyAnimations.AnimateOpacity(ImageProcessingInfo, 1.0, 0, 300);
                                    });
                            }

                            if (FiltersGraph.GetFiltersCount() > 0)
                            {
                                Dispatcher.InvokeAsync(() =>
                                    {
                                        if (Image3.Opacity > 0)
                                            MyAnimations.AnimateOpacity(Image3, 1.0, 0, 300);

                                        if (Grid3.Opacity < 1.0)
                                            MyAnimations.AnimateOpacity(Grid3, 0, 1.0, 300);
                                    });
                            }
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.InvokeAsync(() =>
                                {
                                    LabelLoadError.Content = "Unable to open file: " + ex.Message;

                                    MyAnimations.AnimateOpacity(LabelLoadError, 0, 1.0, 300);

                                    if (LabelSelectDsdiff.Opacity < 1.0)
                                        MyAnimations.AnimateOpacity(LabelSelectDsdiff, 0, 1.0, 300);

                                    if (Grid2.Opacity > 0)
                                        MyAnimations.AnimateOpacity(Grid2, 1.0, 0, 300);

                                    if (Grid3.Opacity > 0)
                                        MyAnimations.AnimateOpacity(Grid3, 1.0, 0, 300);

                                    if (Image1.Opacity > 0)
                                        MyAnimations.AnimateOpacity(Image1, 1.0, 0, 300);
                                });
                        }
                    }, null, true);
        }

        private void AddProcessingMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Grid2.Opacity < 1.0) return;
            if (Grid2.IsEnabled == false) return;

            var grid = (Grid)sender;

            if (Image2.Opacity < 1.0 && Grid2.Opacity > 0)
            {
                Image2.IsEnabled = true;

                MyAnimations.AnimateRenderScale(grid, 1.0, 0.98,
                                                grid.ActualWidth/2, grid.ActualHeight/2, 100, null, null, true);

                MyAnimations.AnimateRenderTranslation(Grid3, TranslateTransform.YProperty,
                                                      -Grid1.ActualHeight - 20, 0, 300, 50);
                MyAnimations.AnimateOpacity(Grid3, 0, 0.7, 100, 50);

                MyAnimations.AnimateOpacity(LabelAddProcessing, 1.0, 0.0, 300);
                MyAnimations.AnimateOpacity(Image2, 0.0, 1.0, 300);
            }
            else
            {
                if (Image2.IsEnabled && (FiltersGraph.GetFiltersCount() < 4 ||
                    FiltersGraph.SelectedFilter != -1))
                {
                    Image2.IsEnabled = false;

                    var edit = new FilterEdit 
                    {
                        ApplyClick = FilterApplyClick,
                        DeleteClick = FilterDeleteClick
                    };

                    if (FiltersGraph.SelectedFilter != -1)
                    {
                        var filter = FiltersGraph.GetFilterDef(FiltersGraph.SelectedFilter);
                        edit.TypeOfFilter = filter.Item1;
                        edit.FrequencyOne = filter.Item2;
                        edit.FrequencyTwo = filter.Item3;
                        edit.EditMode = true;
                    }
                    else
                    {
                        edit.TypeOfFilter = _lastFilterType;
                        edit.FrequencyOne = FiltersGraph.MouseFrequency;
                        edit.FrequencyTwo = edit.FrequencyOne + 50;
                        edit.EditMode = false;
                    }

                    edit.Closed += (o, args) => Image2.IsEnabled = true;
                    MyUtils.ShowCommonDialog(edit, edit.BackGrid, ActualWidth, ActualHeight);
                }
            }
        }

        private void FilterDeleteClick(object sender)
        {
            if (FiltersGraph.SelectedFilter != -1)
            {
                FiltersGraph.DeleteFilterDef(FiltersGraph.SelectedFilter);
            }
        }

        private void FilterApplyClick(object sender)
        {
            MyAnimations.AnimateOpacity(LabelWriteOutput, 0, 1.00, 300);
            MyAnimations.AnimateOpacity(ImageProcessingInfo, 1.0, 0, 300);

            if (Image3.Opacity > 0)
                MyAnimations.AnimateOpacity(Image3, 1.0, 0, 300);

            var filterBox = (FilterEdit) sender;

            _lastFilterType = filterBox.TypeOfFilter;

            var filterColors = new[]
                {
                    Color.FromArgb(150, 244, 164, 96),
                    Color.FromArgb(150, 164, 244, 96),
                    Color.FromArgb(150, 244, 96, 164),
                    Color.FromArgb(150, 96, 164, 244)
                };

            if (FiltersGraph.SelectedFilter != -1)
            {
                FiltersGraph.EditFilterDef(FiltersGraph.SelectedFilter, filterBox.TypeOfFilter,
                                          filterBox.FrequencyOne, filterBox.FrequencyTwo,
                                          filterColors[FiltersGraph.SelectedFilter]);
            }
            else
            {
                FiltersGraph.AddFilterDef(filterBox.TypeOfFilter,
                                          filterBox.FrequencyOne, filterBox.FrequencyTwo,
                                          filterColors[FiltersGraph.GetFiltersCount()]);
            }
        }

        private void WriteOutputMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Grid3.Opacity < 1.0) return;

            var grid = (Grid)sender;
            MyAnimations.AnimateRenderScale(grid, 1.0, 0.98,
                grid.ActualWidth / 2, grid.ActualHeight / 2, 100, (s, t) => Dispatcher.InvokeAsync(() =>
                    {
                        if (ImageProcessingInfo.Opacity < 1.0)
                        {
                            var fileName = "";
                            if (SaveFileDialog(ref fileName) == false) return;

                            _targetFile = fileName;

                            Grid1.IsEnabled = false;
                            Grid2.IsEnabled = false;

                            MyAnimations.AnimateOpacity(LabelWriteOutput, 1.0, 0, 300);
                            MyAnimations.AnimateOpacity(ImageProcessingInfo, 0, 1.0, 300);

                            if (Image3.Opacity > 0)
                                MyAnimations.AnimateOpacity(Image3, 1.0, 0, 300);

                            LabelProcessingPersent.Content = "Processing: 0%";

                            StartProcessing();
                        }
                        else
                        {
                            CloseRunningProcess();
                        }
                    }), null, true);
        }

        ////////////////////////////////////////////////////////////////////////////
        public bool OpenFileDialog(ref string fileName)
        {
            var opendialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "DSDIFF Files (*.dff)|*.dff|All files (*.*)|*.*",
                FilterIndex = 0
            };

            if (opendialog.ShowDialog() != true) return false;

            fileName = opendialog.FileName;

            return true;
        }

        public bool SaveFileDialog(ref string fileName)
        {
            var savedialog = new SaveFileDialog
            {
                Filter = "DSDIFF Files (*.dff)|*.dff|All files (*.*)|*.*",
                FilterIndex = 0
            };

            if (savedialog.ShowDialog() != true) return false;

            fileName = savedialog.FileName;

            return true;
        }

        private void CloseOnMouseDown(object sender, MouseEventArgs e)
        {
            Close();
        }

        private void CloseOnMouseEnter(object sender, MouseEventArgs e)
        {
            var label = (Label) sender;
            label.Foreground = Brushes.White;
        }

        private void CloseOnMouseLeave(object sender, MouseEventArgs e)
        {
            var label = (Label)sender;
            label.Foreground = Brushes.Silver;            
        }

        //////////////////////////////////////////////////////////////////////////

        private void StartProcessing()
        {
            _configFile = WriteConfigFile();

            var runtime = new RuntimeWrap();
            _runningProcess = runtime.StartFiltering(_sourceFile, _targetFile, _configFile);

            _runningProcess.OutputDataReceived += RunningProcessOutputDataReceived;

            _runningProcess.Start();
            _runningProcess.BeginOutputReadLine();

            _runningProcess.Exited += RunningProcessExited;

            _terminateEvent = new EventWaitHandle(false, EventResetMode.ManualReset, "dsdiffCrossoverTerminateEvent");
        }

        void RunningProcessExited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
                {
                    Grid1.IsEnabled = true;
                    Grid2.IsEnabled = true;

                    if (File.Exists(_targetFile))
                    {
                        try
                        {
                            var runtimeWrap = new RuntimeWrap();

                            var fileInfo = new Dictionary<string, string>();
                            var response = runtimeWrap.GetFileResponse(_targetFile, ref fileInfo);

                            var responseConvert =
                                response.Select(
                                    tuple => new PointF((float) tuple.Item1, (float) Math.Log(tuple.Item2)))
                                        .ToArray();

                            ResultGraph.SetPoints(0, responseConvert, Colors.DarkSalmon, "Result");

                            var points = SourceGraph.GetPoints(0);
                            ResultGraph.SetPoints(1, (PointF[]) points.Item1.Clone(), points.Item2, "Source");
                        }
                        catch
                        {
                            Dispatcher.InvokeAsync(() => MyAnimations.AnimateOpacity(TargetInfoError, 0, 1.0, 300));
                        }
                    }

                    if (TargetInfoError.Opacity > 0)
                        MyAnimations.AnimateOpacity(TargetInfoError, 1.0, 0, 300);

                    MyAnimations.AnimateOpacity(ImageProcessingInfo, 1.0, 0, 300);
                    MyAnimations.AnimateOpacity(Image3, 0, 1.0, 300);
                });
        }

        private void CloseRunningProcess()
        {
            if (_runningProcess != null)
            {
                _terminateEvent.Set();

                if (_runningProcess.WaitForExit(3000) == false)
                    _runningProcess.Kill();

                _runningProcess.Dispose();
                _terminateEvent.Dispose();

                File.Delete(_configFile);

                _runningProcess = null;
            }
        }

        void RunningProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;

            if (e.Data.Contains("Written") == false) return;

            var items = e.Data.Split(' ');

            try
            {
                Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            LabelProcessingPersent.Content = "Processing: " + items[1] + "%";
                            LabelProcessingAnimation.Content = _runningAnimation;
                        } catch {}
                    });
            } catch {}

            if (_animationCounter++ > 20)
            {
                _animationCounter = 0;
                _runningAnimation += ".";
                if (_runningAnimation.Length > 4) _runningAnimation = ".";
            }
        }

        private string WriteConfigFile()
        {
            var configFile = Path.GetTempFileName();

            var configData = "{\n    \"channels\":\n    [\n";

            for (var n = 0; n < FiltersGraph.GetFiltersCount(); n++)
            {
                var filter = FiltersGraph.GetFilterDef(n);

                if (n > 0) configData += ",\n";

                var order = 256000;
                if (filter.Item2 > 200) order = 128000;

                configData += "        {\n            \"source\":\"left\",\n            \"filter\":{\"type\":\"";
                configData += filter.Item1.ToString() + "\", \"order\":" + order + ",";

                if (filter.Item1 == FilterEdit.FilterType.BandPass ||
                    filter.Item1 == FilterEdit.FilterType.BandStop)
                {
                    configData += "\"lofreq\": " + filter.Item2 + ", \"hifreq\":" + filter.Item3 + "}";
                }
                else
                {
                    configData += "\"freq\": " + filter.Item2 + "}";
                }

                configData += "\n        },\n";

                configData += "        {\n            \"source\":\"right\",\n            \"filter\":{\"type\":\"";
                configData += filter.Item1.ToString() + "\", \"order\":" + order + ",";

                if (filter.Item1 == FilterEdit.FilterType.BandPass ||
                    filter.Item1 == FilterEdit.FilterType.BandStop)
                {
                    configData += "\"lofreq\": " + filter.Item2 + ", \"hifreq\":" + filter.Item3 + "}";
                }
                else
                {
                    configData += "\"freq\": " + filter.Item2 + "}";
                }

                configData += "\n        }";
            }

            configData += "\n    ]\n}\n";

            File.WriteAllText(configFile, configData);

            return configFile;
        }
    }
}
