using System.Globalization;

namespace GooglePhotosUploader.Localization;

public enum AppLanguage
{
    English,
    Spanish
}

/// <summary>
/// Minimal static localization service. The language is resolved once at startup from
/// the app's "Language" setting ("System", "en-US" or "es-ES"); "System" auto-detects
/// from the OS UI culture (Spanish if the system locale is Spanish, English otherwise).
/// Changing the setting takes effect the next time the app starts.
/// </summary>
public static class Loc
{
    public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

    /// <summary>Resolves <see cref="CurrentLanguage"/> from the given language preference ("System"/"en-US"/"es-ES").</summary>
    public static void Initialize(string languagePreference = "System")
    {
        CurrentLanguage = languagePreference switch
        {
            "en-US" => AppLanguage.English,
            "es-ES" => AppLanguage.Spanish,
            _ => DetectFromSystem()
        };
    }

    private static AppLanguage DetectFromSystem()
    {
        var twoLetterIso = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return twoLetterIso.Equals("es", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Spanish
            : AppLanguage.English;
    }

    public static string Get(string key)
    {
        var table = CurrentLanguage == AppLanguage.Spanish ? Spanish : English;
        if (table.TryGetValue(key, out var value))
        {
            return value;
        }

        return English.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object?[] args) => string.Format(Get(key), args);

    private static readonly Dictionary<string, string> English = new()
    {
        // Tabs
        ["Tab_Dashboard"] = "Dashboard",
        ["Tab_Configuration"] = "Configuration",
        ["Tab_Schedule"] = "Schedule",
        ["Tab_History"] = "History",

        // Common
        ["Common_Choose"] = "Choose...",
        ["Common_Save"] = "Save",

        // Config view
        ["Config_RootFolderLabel"] = "Root folder (each subfolder = one album)",
        ["Config_ClientSecretLabel"] = "client_secret.json file (Google Cloud OAuth)",
        ["Config_ErroredFolderLabel"] = "Discarded files folder (errored)",
        ["Config_BatchSizeLabel"] = "Batch size (BatchSize, max. 50)",
        ["Config_AllowedExtensionsLabel"] = "Allowed extensions (comma-separated)",
        ["Config_ThemeLabel"] = "Theme",
        ["Config_LanguageLabel"] = "Language",
        ["Config_ReauthorizeButton"] = "Reauthorize with Google",
        ["Config_StatusSaved"] = "Configuration saved.",
        ["Config_StatusAuthorizing"] = "Opening the browser to sign in with Google...",
        ["Config_StatusAuthorized"] = "Google session started successfully.",
        ["Config_StatusAuthorizeError"] = "Error signing in: {0}",
        ["Config_StatusLanguageChanged"] = "Language updated. Restart the app for the change to take effect.",
        ["Theme_System"] = "System",
        ["Theme_Light"] = "Light",
        ["Theme_Dark"] = "Dark",
        ["Language_System"] = "System",
        ["Language_English"] = "English",
        ["Language_Spanish"] = "Español",
        ["Picker_SelectRootFolder"] = "Select the root folder",
        ["Picker_SelectClientSecret"] = "Select client_secret.json",
        ["Picker_SelectErroredFolder"] = "Select the discarded files folder",
        ["Auth_MissingClientSecret"] = "Cannot find the OAuth credentials file at '{0}'. Download it from Google Cloud Console (see README.md).",

        // Dashboard view
        ["Dashboard_NoRunYet"] = "No upload has been run yet.",
        ["Dashboard_HistoricalTotal"] = "Historical total uploaded: {0}",
        ["Dashboard_RunNow"] = "Run now",
        ["Dashboard_ReprocessErrors"] = "Reprocess errors",
        ["Dashboard_ProgressByAlbum"] = "Progress by album",
        ["Dashboard_RunLog"] = "Run log",
        ["Dashboard_UploadedOfTotal"] = "{0} / {1} uploaded",
        ["Dashboard_RunInProgress"] = "A run is already in progress (manual or scheduled). Try again in a few minutes.",
        ["Dashboard_LastRunSuccess"] = "Last run: {0} uploaded, {1} discarded (historical: {2}).",
        ["Dashboard_LastRunError"] = "Last run had errors: {0}",
        ["Dashboard_LastReprocessSuccess"] = "Last reprocess run: {0} re-uploaded (historical: {1}).",
        ["Dashboard_LastReprocessError"] = "Last reprocess run had errors: {0}",

        // Schedule view
        ["Schedule_RunDays"] = "Run days",
        ["Schedule_Time"] = "Time",
        ["Schedule_EnableBackground"] = "Enable background execution",
        ["Schedule_SaveSchedule"] = "Save schedule",
        ["Schedule_Note"] = "You must sign in with Google (Configuration tab) before enabling the schedule. The app must be installed in its final location before saving the schedule.",
        ["Schedule_StatusOnlyWinMac"] = "Scheduled execution is only available on Windows and macOS.",
        ["Schedule_StatusNeedSignIn"] = "First sign in with Google from the Configuration tab.",
        ["Schedule_StatusSelectDay"] = "Select at least one day.",
        ["Schedule_StatusSavedNextRun"] = "Schedule saved. Approximate next run: {0}.",
        ["Schedule_StatusDisabled"] = "Background execution disabled.",
        ["Schedule_StatusRegisterError"] = "Error registering the schedule: {0}",
        ["Schedule_StatusActiveNextRun"] = "Active. Approximate next run: {0}.",
        ["Day_Monday"] = "Monday",
        ["Day_Tuesday"] = "Tuesday",
        ["Day_Wednesday"] = "Wednesday",
        ["Day_Thursday"] = "Thursday",
        ["Day_Friday"] = "Friday",
        ["Day_Saturday"] = "Saturday",
        ["Day_Sunday"] = "Sunday",

        // History view
        ["History_Refresh"] = "Refresh",
        ["History_ColStarted"] = "Started",
        ["History_ColOrigin"] = "Origin",
        ["History_ColStatus"] = "Status",
        ["History_ColUploaded"] = "Uploaded",
        ["History_ColDiscarded"] = "Discarded",
        ["History_ColHistorical"] = "Historical",
        ["History_ColError"] = "Error",
        ["RunOrigin_Manual"] = "Manual",
        ["RunOrigin_Scheduled"] = "Scheduled",
        ["RunStatus_Ok"] = "Ok",
        ["RunStatus_QuotaExceeded"] = "Quota exceeded",
        ["RunStatus_Error"] = "Error",
        ["RunStatus_Cancelled"] = "Cancelled",

        // Upload service (run log)
        ["Upload_RootFolderMissing"] = "The root folder '{0}' does not exist.",
        ["Upload_ErrorPrefix"] = "ERROR: {0}",
        ["Upload_FoundFolders"] = "Found {0} folders (= {0} potential albums).",
        ["Upload_CreatingAlbum"] = "Creating album '{0}'...",
        ["Upload_AlbumNothingPending"] = "Album '{0}': nothing pending.",
        ["Upload_AlbumPendingFiles"] = "Album '{0}': {1} pending files.",
        ["Upload_ProgressUploaded"] = "  ... {0} files uploaded in total (historical). Requests today: {1}.",
        ["Upload_SummaryHeader"] = "=== Summary of this run ===",
        ["Upload_SummaryUploaded"] = "Photos/videos uploaded in this run: {0}",
        ["Upload_SummaryDiscarded"] = "Photos/videos discarded (copied to '{0}'): {1}",
        ["Upload_SummaryHistorical"] = "Historical total uploaded: {0}",
        ["Upload_SummaryApiRequests"] = "API requests made today: {0}",
        ["Upload_QuotaWarning"] = "WARNING: {0}",
        ["Upload_QuotaResume"] = "Progress has been saved. Relaunch the application later (or tomorrow) to continue.",
        ["Upload_Cancelled"] = "Run cancelled by the user. Progress has been saved.",
        ["Upload_CancelledMessage"] = "Cancelled by the user",
        ["Upload_UnexpectedError"] = "Unexpected ERROR: {0}",
        ["Upload_Discarded"] = "  ✗ Discarded: {0} ({1})",
        ["Upload_FailuresHeader"] = "Failed photos/videos in this run ({0}):",
        ["Upload_FailureLine"] = "  - [{0}] {1}: {2}",
        ["Upload_ReuploadedSuccess"] = "  ✓ Re-uploaded successfully: {0}",
        ["Upload_CouldNotRemoveErrored"] = "    ⚠ Could not remove '{0}' from the errored folder after a successful re-upload: {1}",
        ["Upload_ErroredFolderMissing"] = "The errored folder '{0}' does not exist.",
        ["Upload_ReprocessFound"] = "Reprocessing errored files: found {0} album folder(s) under '{1}'.",
        ["Upload_ReprocessRetrying"] = "Album '{0}': retrying {1} errored file(s).",
        ["Upload_ReprocessSummaryHeader"] = "=== Summary of this reprocess run ===",
        ["Upload_ReprocessSummaryReuploaded"] = "Photos/videos re-uploaded successfully: {0}",
        ["Upload_ReprocessSummaryStillFailing"] = "Photos/videos still failing (kept in '{0}'): {1}",
        ["Upload_ReprocessCancelled"] = "Reprocess run cancelled by the user. Progress has been saved.",
        ["Upload_StillFailing"] = "  ✗ Still failing, kept in the errored folder: {0} ({1})",
        ["Upload_ReprocessSucceededHeader"] = "Successfully re-uploaded photos/videos ({0}):",
        ["Upload_SucceededLine"] = "  - [{0}] {1}",
        ["Upload_CopySaved"] = "    → Copy saved to '{0}' for manual review (the original was not touched).",
        ["Upload_CouldNotCopy"] = "    ⚠ Could not copy the failed file to '{0}': {1}",
        ["Upload_UnknownConfirmFailure"] = "unknown failure while confirming the media item",
        ["Upload_EmptyApiResponse"] = "Empty or unexpected response from the API",
        ["Quota_ContextCreateAlbum"] = "create the album '{0}'",
        ["Quota_ContextUploadFile"] = "upload the file '{0}'",
        ["Quota_ContextConfirmBatch"] = "confirm a batch of uploaded photos",
        ["Quota_ExceededMessage"] = "Google returned 429 (quota exhausted) while trying to {0}. Stop the application and relaunch it later or tomorrow."
    };

    private static readonly Dictionary<string, string> Spanish = new()
    {
        // Tabs
        ["Tab_Dashboard"] = "Panel",
        ["Tab_Configuration"] = "Configuración",
        ["Tab_Schedule"] = "Programación",
        ["Tab_History"] = "Historial",

        // Common
        ["Common_Choose"] = "Elegir...",
        ["Common_Save"] = "Guardar",

        // Config view
        ["Config_RootFolderLabel"] = "Carpeta raíz (cada subcarpeta = un álbum)",
        ["Config_ClientSecretLabel"] = "Archivo client_secret.json (OAuth de Google Cloud)",
        ["Config_ErroredFolderLabel"] = "Carpeta de archivos descartados (errored)",
        ["Config_BatchSizeLabel"] = "Tamaño de lote (BatchSize, máx. 50)",
        ["Config_AllowedExtensionsLabel"] = "Extensiones permitidas (separadas por comas)",
        ["Config_ThemeLabel"] = "Tema",
        ["Config_LanguageLabel"] = "Idioma",
        ["Config_ReauthorizeButton"] = "Reautorizar con Google",
        ["Config_StatusSaved"] = "Configuración guardada.",
        ["Config_StatusAuthorizing"] = "Abriendo el navegador para iniciar sesión con Google...",
        ["Config_StatusAuthorized"] = "Sesión de Google iniciada correctamente.",
        ["Config_StatusAuthorizeError"] = "Error al iniciar sesión: {0}",
        ["Config_StatusLanguageChanged"] = "Idioma actualizado. Reinicia la aplicación para que el cambio surta efecto.",
        ["Theme_System"] = "Sistema",
        ["Theme_Light"] = "Claro",
        ["Theme_Dark"] = "Oscuro",
        ["Language_System"] = "Sistema",
        ["Language_English"] = "English",
        ["Language_Spanish"] = "Español",
        ["Picker_SelectRootFolder"] = "Selecciona la carpeta raíz",
        ["Picker_SelectClientSecret"] = "Selecciona client_secret.json",
        ["Picker_SelectErroredFolder"] = "Selecciona la carpeta de archivos descartados",
        ["Auth_MissingClientSecret"] = "No se encuentra el archivo de credenciales OAuth en '{0}'. Descárgalo desde Google Cloud Console (consulta README.md).",

        // Dashboard view
        ["Dashboard_NoRunYet"] = "Todavía no se ha ejecutado ninguna subida.",
        ["Dashboard_HistoricalTotal"] = "Total histórico subido: {0}",
        ["Dashboard_RunNow"] = "Ejecutar ahora",
        ["Dashboard_ReprocessErrors"] = "Reprocesar errores",
        ["Dashboard_ProgressByAlbum"] = "Progreso por álbum",
        ["Dashboard_RunLog"] = "Registro de ejecución",
        ["Dashboard_UploadedOfTotal"] = "{0} / {1} subidos",
        ["Dashboard_RunInProgress"] = "Ya hay una ejecución en curso (manual o programada). Inténtalo de nuevo en unos minutos.",
        ["Dashboard_LastRunSuccess"] = "Última ejecución: {0} subidos, {1} descartados (histórico: {2}).",
        ["Dashboard_LastRunError"] = "La última ejecución tuvo errores: {0}",
        ["Dashboard_LastReprocessSuccess"] = "Último reprocesado: {0} vueltos a subir (histórico: {1}).",
        ["Dashboard_LastReprocessError"] = "El último reprocesado tuvo errores: {0}",

        // Schedule view
        ["Schedule_RunDays"] = "Días de ejecución",
        ["Schedule_Time"] = "Hora",
        ["Schedule_EnableBackground"] = "Habilitar ejecución en segundo plano",
        ["Schedule_SaveSchedule"] = "Guardar programación",
        ["Schedule_Note"] = "Debes iniciar sesión con Google (pestaña Configuración) antes de habilitar la programación. La aplicación debe estar instalada en su ubicación final antes de guardar la programación.",
        ["Schedule_StatusOnlyWinMac"] = "La ejecución programada solo está disponible en Windows y macOS.",
        ["Schedule_StatusNeedSignIn"] = "Primero inicia sesión con Google desde la pestaña Configuración.",
        ["Schedule_StatusSelectDay"] = "Selecciona al menos un día.",
        ["Schedule_StatusSavedNextRun"] = "Programación guardada. Próxima ejecución aproximada: {0}.",
        ["Schedule_StatusDisabled"] = "Ejecución en segundo plano deshabilitada.",
        ["Schedule_StatusRegisterError"] = "Error al registrar la programación: {0}",
        ["Schedule_StatusActiveNextRun"] = "Activa. Próxima ejecución aproximada: {0}.",
        ["Day_Monday"] = "Lunes",
        ["Day_Tuesday"] = "Martes",
        ["Day_Wednesday"] = "Miércoles",
        ["Day_Thursday"] = "Jueves",
        ["Day_Friday"] = "Viernes",
        ["Day_Saturday"] = "Sábado",
        ["Day_Sunday"] = "Domingo",

        // History view
        ["History_Refresh"] = "Actualizar",
        ["History_ColStarted"] = "Inicio",
        ["History_ColOrigin"] = "Origen",
        ["History_ColStatus"] = "Estado",
        ["History_ColUploaded"] = "Subidos",
        ["History_ColDiscarded"] = "Descartados",
        ["History_ColHistorical"] = "Histórico",
        ["History_ColError"] = "Error",
        ["RunOrigin_Manual"] = "Manual",
        ["RunOrigin_Scheduled"] = "Programada",
        ["RunStatus_Ok"] = "Correcto",
        ["RunStatus_QuotaExceeded"] = "Cuota excedida",
        ["RunStatus_Error"] = "Error",
        ["RunStatus_Cancelled"] = "Cancelada",

        // Upload service (run log)
        ["Upload_RootFolderMissing"] = "La carpeta raíz '{0}' no existe.",
        ["Upload_ErrorPrefix"] = "ERROR: {0}",
        ["Upload_FoundFolders"] = "Se encontraron {0} carpetas (= {0} álbumes potenciales).",
        ["Upload_CreatingAlbum"] = "Creando álbum '{0}'...",
        ["Upload_AlbumNothingPending"] = "Álbum '{0}': nada pendiente.",
        ["Upload_AlbumPendingFiles"] = "Álbum '{0}': {1} archivos pendientes.",
        ["Upload_ProgressUploaded"] = "  ... {0} archivos subidos en total (histórico). Solicitudes hoy: {1}.",
        ["Upload_SummaryHeader"] = "=== Resumen de esta ejecución ===",
        ["Upload_SummaryUploaded"] = "Fotos/vídeos subidos en esta ejecución: {0}",
        ["Upload_SummaryDiscarded"] = "Fotos/vídeos descartados (copiados a '{0}'): {1}",
        ["Upload_SummaryHistorical"] = "Total histórico subido: {0}",
        ["Upload_SummaryApiRequests"] = "Solicitudes a la API hechas hoy: {0}",
        ["Upload_QuotaWarning"] = "AVISO: {0}",
        ["Upload_QuotaResume"] = "Se ha guardado el progreso. Vuelve a abrir la aplicación más tarde (o mañana) para continuar.",
        ["Upload_Cancelled"] = "Ejecución cancelada por el usuario. Se ha guardado el progreso.",
        ["Upload_CancelledMessage"] = "Cancelada por el usuario",
        ["Upload_UnexpectedError"] = "ERROR inesperado: {0}",
        ["Upload_Discarded"] = "  ✗ Descartado: {0} ({1})",
        ["Upload_FailuresHeader"] = "Fotos/vídeos fallidos en esta ejecución ({0}):",
        ["Upload_FailureLine"] = "  - [{0}] {1}: {2}",
        ["Upload_ReuploadedSuccess"] = "  ✓ Vuelto a subir correctamente: {0}",
        ["Upload_CouldNotRemoveErrored"] = "    ⚠ No se pudo eliminar '{0}' de la carpeta de errores tras subirlo correctamente: {1}",
        ["Upload_ErroredFolderMissing"] = "La carpeta de errores '{0}' no existe.",
        ["Upload_ReprocessFound"] = "Reprocesando archivos con error: se encontraron {0} carpeta(s) de álbum en '{1}'.",
        ["Upload_ReprocessRetrying"] = "Álbum '{0}': reintentando {1} archivo(s) con error.",
        ["Upload_ReprocessSummaryHeader"] = "=== Resumen de este reprocesado ===",
        ["Upload_ReprocessSummaryReuploaded"] = "Fotos/vídeos vueltos a subir correctamente: {0}",
        ["Upload_ReprocessSummaryStillFailing"] = "Fotos/vídeos que siguen fallando (mantenidos en '{0}'): {1}",
        ["Upload_ReprocessCancelled"] = "Reprocesado cancelado por el usuario. Se ha guardado el progreso.",
        ["Upload_StillFailing"] = "  ✗ Sigue fallando, se mantiene en la carpeta de errores: {0} ({1})",
        ["Upload_ReprocessSucceededHeader"] = "Fotos/vídeos vueltos a subir correctamente ({0}):",
        ["Upload_SucceededLine"] = "  - [{0}] {1}",
        ["Upload_CopySaved"] = "    → Copia guardada en '{0}' para revisión manual (el original no se modificó).",
        ["Upload_CouldNotCopy"] = "    ⚠ No se pudo copiar el archivo fallido a '{0}': {1}",
        ["Upload_UnknownConfirmFailure"] = "fallo desconocido al confirmar el elemento multimedia",
        ["Upload_EmptyApiResponse"] = "Respuesta vacía o inesperada de la API",
        ["Quota_ContextCreateAlbum"] = "crear el álbum '{0}'",
        ["Quota_ContextUploadFile"] = "subir el archivo '{0}'",
        ["Quota_ContextConfirmBatch"] = "confirmar un lote de fotos subidas",
        ["Quota_ExceededMessage"] = "Google devolvió 429 (cuota agotada) al intentar {0}. Detén la aplicación y vuelve a abrirla más tarde o mañana."
    };
}
