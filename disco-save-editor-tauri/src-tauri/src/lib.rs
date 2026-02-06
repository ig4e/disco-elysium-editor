mod models;
mod lua_database;
mod states_lua;
mod game_data;
mod save_service;
mod commands;

use commands::AppState;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .manage(AppState::default())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_shell::init())
        .setup(|app| {
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_log::Builder::default()
                        .level(log::LevelFilter::Info)
                        .build(),
                )?;
            }
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::discover_saves,
            commands::pick_save_file,
            commands::load_save,
            commands::save_changes,
            commands::get_lua_variables,
            commands::get_catalog_items,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
