using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using WiiTUIO.Properties;
using System.Threading; // Necesario para Thread.Sleep y el objeto de bloqueo
using System.Windows; // Necesario para MessageBox

using static WiiTUIO.Resources.Resources; // Importa la clase Resources para acceso directo a las cadenas

namespace WiiTUIO.Provider
{
    public class CalibrationSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private float _OffsetYTop = 0.0f;
        public float OffsetYTop 
        { 
            get => _OffsetYTop; 
            set 
            { 
                if (_OffsetYTop == value) return; 
                _OffsetYTop = value; 
                UpdateProfileCalibrationValue("OffsetYTop", value); 
                OnPropertyChanged("OffsetYTop"); 
            } 
        }

        private float _OffsetYBottom = 1.0f;
        public float OffsetYBottom 
        { 
            get => _OffsetYBottom; 
            set 
            { 
                if (_OffsetYBottom == value) return; 
                _OffsetYBottom = value; 
                UpdateProfileCalibrationValue("OffsetYBottom", value); 
                OnPropertyChanged("OffsetYBottom"); 
            } 
        }
        // Propiedades de calibración específicas de la instancia del Wiimote
        private float _Top = 0.0f;
        public float Top
        {
            get => _Top;
            set
            {
                if (_Top == value) return;
                _Top = value;
                UpdateProfileCalibrationValue("Top", value);
                OnPropertyChanged("Top");
            }
        }

        private float _Bottom = 1.0f;
        public float Bottom
        {
            get => _Bottom;
            set
            {
                if (_Bottom == value) return;
                _Bottom = value;
                UpdateProfileCalibrationValue("Bottom", value);
                OnPropertyChanged("Bottom");
            }
        }

        private float _Left = Settings.Default.pointer_4IRMode != "none" ? 0.0f : 1.0f;
        public float Left
        {
            get => _Left;
            set
            {
                if (_Left == value) return;
                _Left = value;
                UpdateProfileCalibrationValue("Left", value);
                OnPropertyChanged("Left");
            }
        }

        private float _Right = Settings.Default.pointer_4IRMode != "none" ? 1.0f : 0.0f;
        public float Right
        {
            get => _Right;
            set
            {
                if (_Right == value) return;
                _Right = value;
                UpdateProfileCalibrationValue("Right", value);
                OnPropertyChanged("Right");
            }
        }

        private float _CenterX = 0.5f;
        public float CenterX
        {
            get => _CenterX;
            set
            {
                if (_CenterX == value) return;
                _CenterX = (float)Math.Min(1.0, Math.Max(0.0, value));
                UpdateProfileCalibrationValue("CenterX", value);
                OnPropertyChanged("CenterX");
            }
        }

        private float _CenterY = 0.5f;
        public float CenterY
        {
            get => _CenterY;
            set
            {
                if (_CenterY == value) return;
                _CenterY = (float)Math.Min(1.0, Math.Max(0.0, value));
                UpdateProfileCalibrationValue("CenterY", value);
                OnPropertyChanged("CenterY");
            }
        }

        private float _TLled = 0.26f;
        public float TLled
        {
            get => _TLled;
            set
            {
                if (_TLled == value) return;
                _TLled = value;
                UpdateProfileCalibrationValue("TLled", value);
                OnPropertyChanged("TLled");
            }
        }

        private float _TRled = 0.74f;
        public float TRled
        {
            get => _TRled;
            set
            {
                if (_TRled == value) return;
                _TRled = value;
                UpdateProfileCalibrationValue("TRled", value);
                OnPropertyChanged("TRled");
            }
        }


        private static string CALIBRATION_FILENAME = System.AppDomain.CurrentDomain.BaseDirectory + "CalibrationData.json";
        private static JObject _rootData; // Estático: Contiene "ActiveProfile" y "Profiles"
        private string _wiimoteID; // Instancia: ID del Wiimote para esta instancia de CalibrationSettings

        // Propiedad estática para el nombre del perfil activo (accesible globalmente)
        private static string _staticActiveProfileName;

        // Evento estático para notificar cambios en las propiedades estáticas
        public static event PropertyChangedEventHandler StaticPropertyChanged;

        // Objeto de bloqueo para sincronizar el acceso al archivo
        private static readonly object _fileLock = new object();

        // Constructor estático: Se ejecuta una sola vez cuando la clase es accedida por primera vez.
        static CalibrationSettings()
        {
            InitializeStaticCalibrationData();
        }

        // Constructor de instancia
        public CalibrationSettings(string wiimoteID)
        {
            _wiimoteID = wiimoteID;
            // InitializeStaticCalibrationData() ya se ha llamado por el constructor estático.
            LoadCalibrationValues(); // Cargar los valores para este Wiimote y el perfil activo
        }

        // Propiedad estática para acceder y modificar el perfil activo
        public static string ActiveProfileName
        {
            get => _staticActiveProfileName;
            set
            {
                if (_staticActiveProfileName == value) return;
                _staticActiveProfileName = value;
                _rootData["ActiveProfile"] = value;
                SaveStaticRootData(); // Guardar el cambio de perfil activo inmediatamente
                OnStaticPropertyChanged("ActiveProfileName");
                OnStaticPropertyChanged("AvailableProfiles"); // La lista de perfiles puede haber cambiado
            }
        }

        // Propiedad estática para la lista de perfiles disponibles
        public static List<string> AvailableProfiles
        {
            get
            {
                if (_rootData?["Profiles"] is JObject profilesObject)
                {
                    return profilesObject.Properties().Select(p => p.Name).ToList();
                }
                return new List<string>();
            }
        }

        // Nuevo método estático privado para guardar el _rootData de forma segura con reintentos
        private static void SaveStaticRootData()
        {
            const int maxRetries = 5;
            const int retryDelayMs = 100; // 100 ms delay
            bool saveSuccessful = false;

            lock (_fileLock)
            {
                string tempFilePath = Path.GetTempFileName();
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        // 1. Escribir a un archivo temporal
                        File.WriteAllText(tempFilePath, _rootData.ToString(Newtonsoft.Json.Formatting.Indented));

                        // 2. Eliminar el archivo original (si existe)
                        if (File.Exists(CALIBRATION_FILENAME))
                        {
                            File.Delete(CALIBRATION_FILENAME);
                        }

                        // 3. Mover el archivo temporal al nombre del original
                        File.Move(tempFilePath, CALIBRATION_FILENAME);

                        Console.WriteLine("CalibrationData.json saved successfully."); // Reverted to non-localized
                        saveSuccessful = true;
                        return; // Éxito, salir del método
                    }
                    catch (IOException ex) // Capturar IOException para problemas de archivo
                    {
                        Console.WriteLine($"Attempt {i + 1}/{maxRetries}: File access error when saving CalibrationData.json: {ex.Message}. Retrying..."); // Reverted to non-localized
                        Thread.Sleep(retryDelayMs); // Esperar antes de reintentar
                    }
                    catch (Exception ex) // Capturar cualquier otra excepción
                    {
                        Console.WriteLine($"Unexpected error when saving CalibrationData.json: {ex.Message}"); // Reverted to non-localized
                        return; // Error no recuperable, salir
                    }
                    finally
                    {
                        // Asegurarse de eliminar el archivo temporal
                        if (File.Exists(tempFilePath))
                        {
                            try { File.Delete(tempFilePath); } catch { } // Ignorar errores al eliminar el temporal
                        }
                    }
                }
                Console.WriteLine($"Failed to save CalibrationData.json after {maxRetries} retries."); // Reverted to non-localized
            }

            if (!saveSuccessful)
            {
                // Mostrar un mensaje al usuario si el guardado falla después de todos los reintentos
                MessageBox.Show(SaveError_Message, SaveError_Title, MessageBoxButton.OK, MessageBoxImage.Error); // Localized strings
            }
        }

        // Método estático para inicializar los datos globales de calibración (incluida la migración) con reintentos
        private static void InitializeStaticCalibrationData()
        {
            // 1. Siempre inicializar _rootData a una estructura base garantizada
            _rootData = new JObject();
            _rootData["Profiles"] = new JObject();
            ((JObject)_rootData["Profiles"])["Default Profile"] = new JObject();
            _rootData["ActiveProfile"] = "Default Profile"; // Perfil activo por defecto

            bool fileExists = File.Exists(CALIBRATION_FILENAME);
            string calText = string.Empty;
            bool readSuccess = false;

            const int maxRetries = 5;
            const int retryDelayMs = 100;

            lock (_fileLock)
            {
                for (int i = 0; i < maxRetries; i++)
                {
                    if (!fileExists) break; // Si el archivo no existe, no hay nada que leer

                    try
                    {
                        calText = File.ReadAllText(CALIBRATION_FILENAME);
                        readSuccess = true;
                        Console.WriteLine("CalibrationData.json read successfully."); // Reverted to non-localized
                        break; // Éxito, salir del bucle de reintentos
                    }
                    catch (IOException ex) // Capturar IOException para problemas de archivo
                    {
                        Console.WriteLine($"Attempt {i + 1}/{maxRetries}: File access error when reading CalibrationData.json during initialization: {ex.Message}. Retrying..."); // Reverted to non-localized
                        Thread.Sleep(retryDelayMs); // Esperar antes de reintentar
                    }
                    catch (Exception ex) // Capturar cualquier otra excepción
                    {
                        Console.WriteLine($"Unexpected error when reading CalibrationData.json during initialization: {ex.Message}"); // Reverted to non-localized
                        readSuccess = false; // Error no recuperable
                        break;
                    }
                }
            }

            bool needsSaveAfterInit = false; // Asumimos que no se necesita guardar a menos que se hagan cambios

            // 3. Procesar el contenido del archivo si está disponible y se leyó correctamente
            if (readSuccess && !string.IsNullOrEmpty(calText))
            {
                try
                {
                    JObject parsedData = JObject.Parse(calText);

                    // Verificar si se necesita migración (formato antiguo)
                    if (parsedData["Profiles"] == null)
                    {
                        Console.WriteLine("Migrating old CalibrationData.json format..."); // Reverted to non-localized
                        JObject defaultProfileData = new JObject();
                        foreach (var prop in parsedData.Properties())
                        {
                            if (prop.Value.Type == JTokenType.Object &&
                                !string.Equals(prop.Name, "ActiveProfile", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(prop.Name, "Profiles", StringComparison.OrdinalIgnoreCase))
                            {
                                defaultProfileData[prop.Name] = prop.Value;
                            }
                        }
                        // Fusionar los datos migrados en el perfil por defecto de nuestra _rootData base
                        ((JObject)_rootData["Profiles"])["Default Profile"] = defaultProfileData;

                        defaultProfileData["pointer_4IRMode"] = Settings.Default.pointer_4IRMode;
                        defaultProfileData["pointer_sensorBarPos"] = Settings.Default.pointer_sensorBarPos;

                        _rootData["ActiveProfile"] = "Default Profile";
                        needsSaveAfterInit = true;
                    }
                    else
                    {
                        // Nuevo formato: Fusionar manualmente las propiedades de nivel superior.
                        // Esto reemplaza las propiedades de _rootData con las de parsedData si existen.
                        // Para "Profiles", si parsedData tiene un objeto de perfiles, lo usamos.
                        if (parsedData["Profiles"] is JObject fileProfiles)
                        {
                            _rootData["Profiles"] = fileProfiles;
                        }
                        // Para "ActiveProfile", si parsedData tiene un valor, lo usamos.
                        if (parsedData["ActiveProfile"] != null)
                        {
                            _rootData["ActiveProfile"] = parsedData["ActiveProfile"];
                        }

                        // Asegurarse de que el perfil activo siga siendo válido después de la fusión
                        // Usamos .Property() para comprobar la existencia de una propiedad sin lanzar excepción si no existe.
                        if (_rootData["ActiveProfile"] == null || !(((JObject)_rootData["Profiles"]).Property(_rootData["ActiveProfile"].ToString()) != null))
                        {
                            _rootData["ActiveProfile"] = "Default Profile";
                            needsSaveAfterInit = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing CalibrationData.json: {ex.Message}. Default configuration will be used."); // Reverted to non-localized
                    needsSaveAfterInit = true; // Forzar el guardado de la estructura por defecto si el parseo falla
                }
            }
            else if (fileExists && !readSuccess) // Si el archivo existía pero no se pudo leer después de reintentos
            {
                Console.WriteLine($"Failed to read CalibrationData.json after {maxRetries} retries. Default configuration will be used."); // Reverted to non-localized
                needsSaveAfterInit = true; // Forzar el guardado de la estructura por defecto
            }
            else // Archivo no existe o estaba vacío
            {
                Console.WriteLine("CalibrationData.json not found or empty. Initializing with default configuration."); // Reverted to non-localized
                needsSaveAfterInit = true; // Forzar el guardado de la estructura por defecto
            }

            // 4. Comprobaciones finales para asegurar la consistencia (redundante pero seguro)
            if (_rootData["Profiles"]["Default Profile"] == null)
            {
                ((JObject)_rootData["Profiles"])["Default Profile"] = new JObject();
                ((JObject)_rootData["Profiles"])["Default Profile"]["pointer_4IRMode"] = Settings.Default.pointer_4IRMode;
                ((JObject)_rootData["Profiles"])["Default Profile"]["pointer_sensorBarPos"] = Settings.Default.pointer_sensorBarPos;

                needsSaveAfterInit = true;
            }

            _staticActiveProfileName = _rootData["ActiveProfile"]?.ToString() ?? "Default Profile";
            _rootData["ActiveProfile"] = _staticActiveProfileName; // Asegurarse de que esté establecido en el JObject

            if (needsSaveAfterInit)
            {
                SaveStaticRootData(); // Llamar al método estático para guardar
            }

            // 5. Disparar eventos
            OnStaticPropertyChanged("AvailableProfiles");
            OnStaticPropertyChanged("ActiveProfileName");
        }

        // Método de instancia para obtener el JObject del perfil activo
        private JObject GetOrCreateCurrentProfileJObject()
        {
            JObject profiles = (JObject)_rootData["Profiles"];
            if (profiles == null)
            {
                profiles = new JObject();
                _rootData["Profiles"] = profiles;
            }
            if (profiles[_staticActiveProfileName] == null) // Usar el nombre de perfil estático
            {
                profiles[_staticActiveProfileName] = new JObject();
                profiles[_staticActiveProfileName]["pointer_4IRMode"] = Settings.Default.pointer_4IRMode;
                profiles[_staticActiveProfileName]["pointer_sensorBarPos"] = Settings.Default.pointer_sensorBarPos;

            }
            return (JObject)profiles[_staticActiveProfileName];
        }

        // Método de instancia para actualizar un valor de calibración en el perfil activo y Wiimote ID
        private void UpdateProfileCalibrationValue(string propertyName, object value)
        {
            JObject currentProfile = GetOrCreateCurrentProfileJObject();
            if (currentProfile[_wiimoteID] == null)
            {
                currentProfile[_wiimoteID] = new JObject();
            }
            ((JObject)currentProfile[_wiimoteID])[propertyName] = JToken.FromObject(value);
            SaveCalibrationData(); // Llamar al guardado de instancia
        }

        // Método de instancia para cargar los valores de calibración para este Wiimote y el perfil activo
        private void LoadCalibrationValues()
        {
            JObject currentProfile = GetOrCreateCurrentProfileJObject();
            JObject wiimoteData = (JObject)currentProfile[_wiimoteID]; // Intenta obtener los datos del Wiimote específico

            bool needsSave = false;
            // Si no se encuentran datos para el Wiimote específico,
            // se inicializará con los valores por defecto de C# y se creará una nueva entrada en el JSON.
            if (wiimoteData == null)
            {
                Console.WriteLine($"Wiimote ID '{_wiimoteID}' not found in active profile '{_staticActiveProfileName}'. Initializing with default C# values.");
                currentProfile[_wiimoteID] = new JObject(); // Crea un JObject vacío para este Wiimote específico
                wiimoteData = (JObject)currentProfile[_wiimoteID]; // Apunta wiimoteData al objeto vacío recién creado
                needsSave = true;
            }
            else
            {
                Console.WriteLine($"Loaded calibration data for Wiimote ID '{_wiimoteID}' from active profile '{_staticActiveProfileName}'.");
            }

            var propertiesToLoad = new List<string>
            {
                "Top", "Bottom", "Left", "Right",
                "CenterX", "CenterY", "TLled", "TRled", "OffsetYTop", "OffsetYBottom"
            };

            foreach (var propertyName in propertiesToLoad)
            {
                var propertyInfo = GetType().GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    // Si el valor existe en el JSON para esta propiedad, cárgalo.
                    if (wiimoteData[propertyName] != null)
                    {
                        var value = wiimoteData[propertyName].ToObject(propertyInfo.PropertyType);
                        propertyInfo.SetValue(this, value);
                    }
                    else
                    {
                        // Si la propiedad no existe en el JSON (o es un nuevo Wiimote),
                        // se usará el valor por defecto de C# y se añadirá al JObject para futuras guardadas.
                        var defaultValue = propertyInfo.GetValue(this);
                        wiimoteData[propertyName] = JToken.FromObject(defaultValue);
                        Console.WriteLine($"Property '{propertyName}' not found for Wiimote ID '{_wiimoteID}'. Using C# default and adding to JSON.");
                        needsSave = true;
                    }
                }
            }
            if (needsSave)
            {
                SaveCalibrationData();
                Console.WriteLine($"Saving new Wiimote ID '{_wiimoteID}' and/or default values to profile '{_staticActiveProfileName}'.");
            }
        }

        // Método de instancia para guardar los datos globales de calibración
        // Ahora es de instancia, pero llama al método estático SaveStaticRootData.
        public void SaveCalibrationData()
        {
            SaveStaticRootData();
        }

        // Método estático para crear un nuevo perfil
        public static void CreateNewProfile(string profileName, string pointer_4IRMode, string pointer_sensorBarPos)
        {
            Console.WriteLine($"Attempting to create new profile: {profileName}"); // Reverted to non-localized
            if (string.IsNullOrWhiteSpace(profileName))
            {
                throw new ArgumentException(Profile_NameEmpty); // Localized string
            }
            if (AvailableProfiles.Contains(profileName))
            {
                throw new ArgumentException(string.Format(Profile_Exists), profileName); // Localized string
            }

            JObject profiles = (JObject)_rootData["Profiles"];
            if (profiles == null)
            {
                profiles = new JObject();
                _rootData["Profiles"] = profiles;
            }

            // Crear un nuevo perfil vacío por defecto.
            JObject newProfile = new JObject();
            newProfile["pointer_4IRMode"] = pointer_4IRMode;
            newProfile["pointer_sensorBarPos"] = pointer_sensorBarPos;

            profiles[profileName] = newProfile;

            Console.WriteLine($"Profile '{profileName}' added to in-memory structure. _rootData content before updating ActiveProfile: {_rootData.ToString(Newtonsoft.Json.Formatting.Indented)}"); // Reverted to non-localized

            // Actualizar el perfil activo. Esto disparará SaveStaticRootData y OnStaticPropertyChanged.
            ActiveProfileName = profileName;
            Console.WriteLine($"Active profile set to: {ActiveProfileName}. SaveStaticRootData is expected to have been called."); // Reverted to non-localized

            // Llamada explícita a SaveStaticRootData para asegurar la persistencia inmediata.
            // Esto es redundante si ActiveProfileName ya lo llama, pero es un seguro.
            SaveStaticRootData();
            Console.WriteLine($"Explicit save of CalibrationData.json after profile creation. Final _rootData content: {_rootData.ToString(Newtonsoft.Json.Formatting.Indented)}"); // Reverted to non-localized

            // Asegurarse de que la UI se actualice después de la operación.
            OnStaticPropertyChanged("AvailableProfiles");
            OnStaticPropertyChanged("ActiveProfileName");
        }

        // Método estático para eliminar un perfil
        public static void DeleteProfile(string profileName)
        {
            Console.WriteLine($"Attempting to delete profile: {profileName}"); // Reverted to non-localized
            if (profileName == "Default Profile")
            {
                throw new ArgumentException(Profile_CannotDeleteDefault); // Localized string
            }
            if (!AvailableProfiles.Contains(profileName))
            {
                throw new ArgumentException(string.Format(Profile_DoesNotExist), profileName); // Localized string
            }

            JObject profiles = (JObject)_rootData["Profiles"];
            if (profiles != null)
            {
                profiles.Remove(profileName);
                Console.WriteLine($"Profile '{profileName}' removed from in-memory structure. _rootData content before updating ActiveProfile: {_rootData.ToString(Newtonsoft.Json.Formatting.Indented)}"); // Reverted to non-localized

                if (_staticActiveProfileName == profileName) // Si se elimina el perfil activo, cambiar a "Default"
                {
                    ActiveProfileName = "Default Profile"; // Esto disparará SaveStaticRootData
                    Console.WriteLine($"Active profile changed to 'Default Profile' after deleting '{profileName}'."); // Reverted to non-localized
                }
                else
                {
                    SaveStaticRootData(); // Si el perfil activo no cambia, guardamos explícitamente.
                    Console.WriteLine($"Explicit save of CalibrationData.json after deleting profile (active not changed). Final _rootData content: {_rootData.ToString(Newtonsoft.Json.Formatting.Indented)}"); // Reverted to non-localized
                }
                OnStaticPropertyChanged("AvailableProfiles"); // Notificar a la UI que la lista de perfiles ha cambiado
            }
        }
        public static (string pointer_4IRMode, string pointer_sensorBarPos) GetLayoutForProfile(string profileName)
        {
            if (_rootData?["Profiles"] is JObject profiles && profiles[profileName] is JObject profileData)
            {
                // Si el perfil existe pero es antiguo (no tiene las claves), usará los valores por defecto.
                string mode = profileData["pointer_4IRMode"]?.ToString() ?? "none";
                string pos = profileData["pointer_sensorBarPos"]?.ToString() ?? "`top";
                return (mode, pos);
            }

            // Fallback si el perfil no se encuentra (aunque no debería pasar si la lista está sincronizada)
            // o si el perfil es antiguo.
            return ("none", "top");
        }
        // Dispara el evento PropertyChanged para las propiedades de instancia
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Dispara el evento StaticPropertyChanged para las propiedades estáticas
        protected static void OnStaticPropertyChanged(string name)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
        }
    }
}
