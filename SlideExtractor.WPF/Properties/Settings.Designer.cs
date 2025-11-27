namespace SlideExtractor.WPF.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.8.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string LastVideoPath {
            get => ((string)this["LastVideoPath"]) ?? string.Empty;
            set => this["LastVideoPath"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public global::System.Collections.Specialized.StringCollection RecentFiles {
            get => this["RecentFiles"] as global::System.Collections.Specialized.StringCollection 
                   ?? new global::System.Collections.Specialized.StringCollection();
            set => this["RecentFiles"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public int FrameInterval {
            get => this["FrameInterval"] is int value ? value : 30;
            set => this["FrameInterval"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public int HashThreshold {
            get => this["HashThreshold"] is int value ? value : 5;
            set => this["HashThreshold"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string TesseractPath {
            get => ((string)this["TesseractPath"]) ?? string.Empty;
            set => this["TesseractPath"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string OutputDirectory {
            get => ((string)this["OutputDirectory"]) ?? string.Empty;
            set => this["OutputDirectory"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public double WindowWidth {
            get => this["WindowWidth"] is double value ? value : 1280d;
            set => this["WindowWidth"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public double WindowHeight {
            get => this["WindowHeight"] is double value ? value : 800d;
            set => this["WindowHeight"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public double WindowLeft {
            get => this["WindowLeft"] is double value ? value : 100d;
            set => this["WindowLeft"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public double WindowTop {
            get => this["WindowTop"] is double value ? value : 100d;
            set => this["WindowTop"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string Theme {
            get => ((string)this["Theme"]) ?? "Dark";
            set => this["Theme"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public bool AutoSaveEnabled {
            get => this["AutoSaveEnabled"] is bool value ? value : true;
            set => this["AutoSaveEnabled"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public bool DragDropEnabled {
            get => this["DragDropEnabled"] is bool value ? value : true;
            set => this["DragDropEnabled"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public bool IsFirstRun {
            get => this["IsFirstRun"] is bool value ? value : true;
            set => this["IsFirstRun"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string UiLanguage {
            get => ((string)this["UiLanguage"]) ?? "zh-CN";
            set => this["UiLanguage"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string LastExportFormat {
            get => ((string)this["LastExportFormat"]) ?? "PPTX";
            set => this["LastExportFormat"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string OcrLanguages {
            get => ((string)this["OcrLanguages"]) ?? "eng";
            set => this["OcrLanguages"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public int ThumbnailSize {
            get => this["ThumbnailSize"] is int value ? value : 180;
            set => this["ThumbnailSize"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public int ThumbnailColumns {
            get => this["ThumbnailColumns"] is int value ? value : 4;
            set => this["ThumbnailColumns"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public string ImageFormat {
            get => ((string)this["ImageFormat"]) ?? "JPG";
            set => this["ImageFormat"] = value;
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        public int ImageQuality {
            get => this["ImageQuality"] is int value ? value : 90;
            set => this["ImageQuality"] = value;
        }
    }
}
