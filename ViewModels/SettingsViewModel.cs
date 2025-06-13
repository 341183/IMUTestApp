using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using IMUTestApp.Models;
using IMUTestApp.Services;

namespace IMUTestApp.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _settingsService;
        private readonly ConfigService _configService; // 添加ConfigService
        private string _statusMessage = "";
        private bool _hasUnsavedChanges = false;
        
        public SettingsViewModel(SettingsService settingsService, ConfigService configService)
        {
            _settingsService = settingsService;
            _configService = configService; // 注入ConfigService
            Settings = _settingsService.Settings;
            
            // 监听设置变化
            Settings.PropertyChanged += (s, e) => HasUnsavedChanges = true;
            
            BrowseDataPathCommand = new RelayCommand(BrowseDataPath);
            BrowseLogPathCommand = new RelayCommand(BrowseLogPath);
            BrowseBackupPathCommand = new RelayCommand(BrowseBackupPath);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ResetSettingsCommand = new RelayCommand(ResetSettings);
            ValidatePathsCommand = new RelayCommand(ValidatePaths);
        }
        
        public AppSettings Settings { get; }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }
        
        public ICommand BrowseDataPathCommand { get; }
        public ICommand BrowseLogPathCommand { get; }
        public ICommand BrowseBackupPathCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public ICommand ValidatePathsCommand { get; }
        
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
                Settings.DataPath = Path.GetDirectoryName(dialog.FileName) ?? Settings.DataPath;
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
                Settings.LogPath = Path.GetDirectoryName(dialog.FileName) ?? Settings.LogPath;
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
                Settings.BackupPath = Path.GetDirectoryName(dialog.FileName) ?? Settings.BackupPath;
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                // 保存用户设置
                _settingsService.SaveSettings();
                
                // 同步更新config.json中的相关配置
                SyncToConfig();
                
                HasUnsavedChanges = false;
                StatusMessage = "设置已保存";
                
                // 3秒后清除状态消息
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ => StatusMessage = "");
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }
        
        private void SyncToConfig()
        {
            try
            {
                // 将用户设置同步到config.json
                _configService.Config.GeneralSettings.LogPath = Settings.LogPath;
                _configService.Config.GeneralSettings.DataPath = Settings.DataPath;
                _configService.Config.GeneralSettings.EnableLogRotation = Settings.EnableLogRotation;
                _configService.Config.GeneralSettings.LogRotationDays = Settings.LogRotationDays;
                _configService.Config.GeneralSettings.MaxLogFileSize = Settings.MaxLogFileSize;
                _configService.Config.GeneralSettings.AutoCreateDirectories = Settings.AutoCreateDirectories;
                _configService.Config.GeneralSettings.DataFileFormat = Settings.DataFileFormat;
                
                // 保存config.json
                _configService.SaveConfig();
            }
            catch (Exception ex)
            {
                throw new Exception($"同步配置文件失败: {ex.Message}");
            }
        }
        
        private void ResetSettings()
        {
            try
            {
                _settingsService.ResetToDefaults();
                
                // 同步重置config.json
                SyncToConfig();
                
                HasUnsavedChanges = false;
                StatusMessage = "设置已重置为默认值";
                
                // 3秒后清除状态消息
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ => StatusMessage = "");
            }
            catch (Exception ex)
            {
                StatusMessage = $"重置失败: {ex.Message}";
            }
        }
        
        private void ValidatePaths()
        {
            try
            {
                if (_settingsService.ValidatePaths())
                {
                    StatusMessage = "路径验证成功";
                }
                else
                {
                    StatusMessage = "路径验证失败，请检查路径设置";
                }
                
                // 3秒后清除状态消息
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ => StatusMessage = "");
            }
            catch (Exception ex)
            {
                StatusMessage = $"验证失败: {ex.Message}";
            }
        }
    }
}