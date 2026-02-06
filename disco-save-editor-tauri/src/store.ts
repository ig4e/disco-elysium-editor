import { create } from "zustand";
import { invoke } from "@tauri-apps/api/core";
import type {
  FullSaveState,
  SaveSummary,
  CatalogItem,
  LuaVariableDisplay,
  SaveUpdatePayload,
} from "./types";

interface AppStore {
  // UI
  darkMode: boolean;
  toggleDarkMode: () => void;

  // Saves list
  saves: SaveSummary[];
  savesLoading: boolean;
  discoverSaves: () => Promise<void>;
  locateSaveFile: () => Promise<void>;

  // Current save
  currentSave: FullSaveState | null;
  saveLoading: boolean;
  dirty: boolean;
  loadSave: (path: string) => Promise<void>;

  // Lua variables (loaded on demand)
  luaVariables: LuaVariableDisplay[];
  luaLoading: boolean;
  loadLuaVariables: (filter?: string) => Promise<void>;

  // Catalog items
  catalogItems: CatalogItem[];
  catalogLoading: boolean;
  loadCatalogItems: () => Promise<void>;

  // Mutations
  luaEdits: Record<string, string>;
  resetCheckKeys: string[];
  resetSeenCheckKeys: string[];
  updateField: <K extends keyof FullSaveState>(
    key: K,
    value: FullSaveState[K]
  ) => void;
  setLuaEdit: (key: string, value: string) => void;
  addResetCheck: (key: string) => void;
  removeResetCheck: (key: string) => void;
  addResetSeenCheck: (key: string) => void;
  removeResetSeenCheck: (key: string) => void;

  // Save to disk
  saving: boolean;
  saveChanges: () => Promise<void>;

  // Error
  error: string | null;
  clearError: () => void;
}

export const useStore = create<AppStore>((set, get) => ({
  darkMode: true,
  toggleDarkMode: () => {
    set((s) => {
      const next = !s.darkMode;
      if (next) document.documentElement.classList.add("dark");
      else document.documentElement.classList.remove("dark");
      return { darkMode: next };
    });
  },

  saves: [],
  savesLoading: false,
  discoverSaves: async () => {
    set({ savesLoading: true, error: null });
    try {
      const saves = await invoke<SaveSummary[]>("discover_saves");
      set({ saves, savesLoading: false });
    } catch (e) {
      set({ savesLoading: false, error: String(e) });
    }
  },
  locateSaveFile: async () => {
    try {
      const path = await invoke<string | null>("pick_save_file");
      if (path) {
        await get().loadSave(path);
      }
    } catch (e) {
      set({ error: String(e) });
    }
  },

  currentSave: null,
  saveLoading: false,
  dirty: false,
  loadSave: async (path: string) => {
    set({ saveLoading: true, error: null, dirty: false, luaEdits: {}, resetCheckKeys: [], resetSeenCheckKeys: [] });
    try {
      const state = await invoke<FullSaveState>("load_save", {
        folderPath: path,
      });
      set({ currentSave: state, saveLoading: false });
    } catch (e) {
      set({ saveLoading: false, error: String(e) });
    }
  },

  luaVariables: [],
  luaLoading: false,
  loadLuaVariables: async (filter?: string) => {
    set({ luaLoading: true });
    try {
      const vars = await invoke<LuaVariableDisplay[]>("get_lua_variables", {
        filter: filter || null,
      });
      set({ luaVariables: vars, luaLoading: false });
    } catch (e) {
      set({ luaLoading: false, error: String(e) });
    }
  },

  catalogItems: [],
  catalogLoading: false,
  loadCatalogItems: async () => {
    set({ catalogLoading: true });
    try {
      const items = await invoke<CatalogItem[]>("get_catalog_items");
      set({ catalogItems: items, catalogLoading: false });
    } catch (e) {
      set({ catalogLoading: false, error: String(e) });
    }
  },

  luaEdits: {},
  resetCheckKeys: [],
  resetSeenCheckKeys: [],
  updateField: (key, value) => {
    set((s) => {
      if (!s.currentSave) return s;
      return {
        currentSave: { ...s.currentSave, [key]: value },
        dirty: true,
      };
    });
  },
  setLuaEdit: (key, value) => {
    set((s) => ({
      luaEdits: { ...s.luaEdits, [key]: value },
      dirty: true,
    }));
  },
  addResetCheck: (key) => {
    set((s) => ({
      resetCheckKeys: [...s.resetCheckKeys, key],
      dirty: true,
    }));
  },
  removeResetCheck: (key) => {
    set((s) => ({
      resetCheckKeys: s.resetCheckKeys.filter((k) => k !== key),
    }));
  },
  addResetSeenCheck: (key) => {
    set((s) => ({
      resetSeenCheckKeys: [...s.resetSeenCheckKeys, key],
      dirty: true,
    }));
  },
  removeResetSeenCheck: (key) => {
    set((s) => ({
      resetSeenCheckKeys: s.resetSeenCheckKeys.filter((k) => k !== key),
    }));
  },

  saving: false,
  saveChanges: async () => {
    const s = get();
    if (!s.currentSave) return;
    set({ saving: true, error: null });
    try {
      const payload: SaveUpdatePayload = {
        folder_path: s.currentSave.folder_path,
        base_name: s.currentSave.base_name,
        xp_amount: s.currentSave.xp_amount,
        level: s.currentSave.level,
        skill_points: s.currentSave.skill_points,
        money: s.currentSave.money,
        health: s.currentSave.health,
        morale: s.currentSave.morale,
        day: s.currentSave.day,
        hours: s.currentSave.hours,
        minutes: s.currentSave.minutes,
        abilities: s.currentSave.abilities,
        skills: s.currentSave.skills,
        owned_items: s.currentSave.owned_items,
        bullets: s.currentSave.bullets,
        thoughts: s.currentSave.thoughts,
        area_id: s.currentSave.area_id,
        party_state: s.currentSave.party_state,
        hud_state: s.currentSave.hud_state,
        game_mode: s.currentSave.game_mode,
        location_flags: s.currentSave.location_flags,
        weather_preset: s.currentSave.weather_preset,
        reputation: s.currentSave.reputation,
        lua_edits: s.luaEdits,
        reset_check_keys: s.resetCheckKeys,
        reset_seen_check_keys: s.resetSeenCheckKeys,
        door_states: s.currentSave.door_states,
        area_states: s.currentSave.area_states,
        shown_orbs: s.currentSave.shown_orbs,
      };
      await invoke("save_changes", { payload });
      set({ saving: false, dirty: false, luaEdits: {}, resetCheckKeys: [], resetSeenCheckKeys: [] });
    } catch (e) {
      set({ saving: false, error: String(e) });
    }
  },

  error: null,
  clearError: () => set({ error: null }),
}));
