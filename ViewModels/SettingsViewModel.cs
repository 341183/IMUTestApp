using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using IMUTestApp.Services;

namespace IMUTestApp.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ConfigService _configService;
        private string _statusMessage = string.Empty;
        private bool _hasUnsavedChanges = false;
        
        public SettingsViewModel(ConfigService configService)
        {
            _configService = configService;
            
            BrowseDataPathCommand = new RelayCommand(BrowseDataPath);
            BrowseLogPathCommand = new RelayCommand(BrowseLogPath);
            BrowseBackupPathCommand = new RelayCommand(BrowseBackupPath);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ValidatePathsCommand = new RelayCommand(ValidatePaths);
            ResetSettingsCommand = new RelayCommand(ResetSettings);
        }
        
        public ICommand BrowseDataPathCommand { get; }
        public ICommand BrowseLogPathCommand { get; }
        public ICommand BrowseBackupPathCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ValidatePathsCommand { get; }
        public ICommand ResetSettingsCommand { get; }

        public string DataPath
        {
            get => _configService.Config.GeneralSettings.DataPath;
            set
            {
                _configService.Config.GeneralSettings.DataPath = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public string BackupPath
        {
            get => _configService.Config.GeneralSettings.BackupPath;
            set
            {
                _configService.Config.GeneralSettings.BackupPath = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public string LogPath
        {
            get => _configService.Config.GeneralSettings.LogPath;
            set
            {
                _configService.Config.GeneralSettings.LogPath = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public string DataFileFormat
        {
            get => _configService.Config.GeneralSettings.DataFileFormat;
            set
            {
                _configService.Config.GeneralSettings.DataFileFormat = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public bool EnableDataBackup
        {
            get => _configService.Config.GeneralSettings.EnableDataBackup;
            set
            {
                _configService.Config.GeneralSettings.EnableDataBackup = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public bool EnableLogRotation
        {
            get => _configService.Config.GeneralSettings.EnableLogRotation;
            set
            {
                _configService.Config.GeneralSettings.EnableLogRotation = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public int LogRotationDays
        {
            get => _configService.Config.GeneralSettings.LogRotationDays;
            set
            {
                _configService.Config.GeneralSettings.LogRotationDays = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public int MaxLogFileSize
        {
            get => _configService.Config.GeneralSettings.MaxLogFileSize;
            set
            {
                _configService.Config.GeneralSettings.MaxLogFileSize = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public bool AutoCreateDirectories
        {
            get => _configService.Config.GeneralSettings.AutoCreateDirectories;
            set
            {
                _configService.Config.GeneralSettings.AutoCreateDirectories = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                OnPropertyChanged();
            }
        }
        
        private void BrowseDataPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择数据保存路径",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };
            
            if (dialog.ShowDialog() == true)
            {
                DataPath = Path.GetDirectoryName(dialog.FileName) ?? _configService.Config.GeneralSettings.DataPath;
            }
        }
        
        private void BrowseLogPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择日志保存路径",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };
            
            if (dialog.ShowDialog() == true)
            {
                LogPath = Path.GetDirectoryName(dialog.FileName) ?? _configService.Config.GeneralSettings.LogPath;
            }
        }
        
        private void BrowseBackupPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择备份保存路径",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };
            
            if (dialog.ShowDialog() == true)
            {
                BackupPath = Path.GetDirectoryName(dialog.FileName) ?? _configService.Config.GeneralSettings.BackupPath;
            }
        }

        private void SaveSettings()
        {
            try
            {
                _configService.SaveConfig();
                StatusMessage = "设置保存成功！";
                HasUnsavedChanges = false;
                
                // 3秒后清除状态消息
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ => 
                {
                    StatusMessage = string.Empty;
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }

        private void ValidatePaths()
        {
            var invalidPaths = new System.Collections.Generic.List<string>();
            
            if (!Directory.Exists(DataPath))
                invalidPaths.Add("数据路径");
            if (!Directory.Exists(LogPath))
                invalidPaths.Add("日志路径");
            if (EnableDataBackup && !Directory.Exists(BackupPath))
                invalidPaths.Add("备份路径");
            
            if (invalidPaths.Count == 0)
            {
                StatusMessage = "所有路径验证通过！";
            }
            else
            {
                StatusMessage = $"以下路径无效: {string.Join(", ", invalidPaths)}";
            }
        }

        private void ResetSettings()
        {
            var defaultConfig = new Models.GeneralSettings();
            DataPath = defaultConfig.DataPath;
            LogPath = defaultConfig.LogPath;
            BackupPath = defaultConfig.BackupPath;
            DataFileFormat = defaultConfig.DataFileFormat;
            EnableDataBackup = defaultConfig.EnableDataBackup;
            EnableLogRotation = defaultConfig.EnableLogRotation;
            LogRotationDays = defaultConfig.LogRotationDays;
            MaxLogFileSize = defaultConfig.MaxLogFileSize;
            AutoCreateDirectories = defaultConfig.AutoCreateDirectories;
            
            StatusMessage = "设置已重置为默认值";
        }
    }
}