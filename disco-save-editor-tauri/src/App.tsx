import { useEffect, useState } from "react";
import { useStore } from "./store";
import { TooltipProvider } from "./components/ui/tooltip";
import { Button } from "./components/ui/button";
import { ScrollArea } from "./components/ui/scroll-area";
import { Separator } from "./components/ui/separator";
import { Badge } from "./components/ui/badge";
import {
  User, Backpack, Brain, BookOpen, Users, Globe, RotateCcw,
  Package, Layers, Save, FolderOpen, Moon, Sun, Loader2, AlertTriangle
} from "lucide-react";

import HomePage from "./pages/HomePage";
import CharacterPage from "./pages/CharacterPage";
import InventoryPage from "./pages/InventoryPage";
import ThoughtCabinetPage from "./pages/ThoughtCabinetPage";
import JournalPage from "./pages/JournalPage";
import PartyPage from "./pages/PartyPage";
import WorldPage from "./pages/WorldPage";
import WhiteChecksPage from "./pages/WhiteChecksPage";
import ContainersPage from "./pages/ContainersPage";
import StatesPage from "./pages/StatesPage";

const PAGES = [
  { id: "character", label: "Character", icon: User },
  { id: "inventory", label: "Inventory", icon: Backpack },
  { id: "thoughts", label: "Thought Cabinet", icon: Brain },
  { id: "journal", label: "Journal", icon: BookOpen },
  { id: "party", label: "Party", icon: Users },
  { id: "world", label: "World", icon: Globe },
  { id: "whitechecks", label: "White Checks", icon: RotateCcw },
  { id: "containers", label: "Containers", icon: Package },
  { id: "states", label: "States", icon: Layers },
] as const;

type PageId = (typeof PAGES)[number]["id"];

export default function App() {
  const {
    currentSave, dirty, saving, saveLoading, error, darkMode,
    toggleDarkMode, saveChanges, clearError
  } = useStore();
  const [activePage, setActivePage] = useState<PageId | "home">("home");

  useEffect(() => {
    if (!currentSave) setActivePage("home");
  }, [currentSave]);

  const renderPage = () => {
    if (!currentSave && activePage !== "home") return null;
    switch (activePage) {
      case "home": return <HomePage onLoaded={() => setActivePage("character")} />;
      case "character": return <CharacterPage />;
      case "inventory": return <InventoryPage />;
      case "thoughts": return <ThoughtCabinetPage />;
      case "journal": return <JournalPage />;
      case "party": return <PartyPage />;
      case "world": return <WorldPage />;
      case "whitechecks": return <WhiteChecksPage />;
      case "containers": return <ContainersPage />;
      case "states": return <StatesPage />;
    }
  };

  return (
    <TooltipProvider>
      <div className="flex h-screen bg-background text-foreground overflow-hidden">
        {/* Sidebar */}
        <div className="w-56 flex flex-col border-r bg-card">
          <div className="p-4 flex items-center gap-2">
            <Brain className="h-6 w-6 text-primary" />
            <span className="font-bold text-sm">Disco Save Editor</span>
          </div>
          <Separator />

          {/* Nav links */}
          <ScrollArea className="flex-1 w-full">
            <div className="p-2 space-y-1">
              <Button
                variant={activePage === "home" ? "secondary" : "ghost"}
                className="w-full justify-start gap-2 text-sm"
                onClick={() => setActivePage("home")}
              >
                <FolderOpen className="h-4 w-4" />
                Saves
              </Button>
              <Separator className="my-2" />
              {PAGES.map((p) => (
                <Button
                  key={p.id}
                  variant={activePage === p.id ? "secondary" : "ghost"}
                  className="w-full justify-start gap-2 text-sm"
                  disabled={!currentSave}
                  onClick={() => setActivePage(p.id)}
                >
                  <p.icon className="h-4 w-4" />
                  {p.label}
                </Button>
              ))}
            </div>
          </ScrollArea>

          {/* Bottom bar */}
          <Separator />
          <div className="p-2 space-y-2">
            {dirty && (
              <Button
                className="w-full gap-2"
                onClick={saveChanges}
                disabled={saving}
              >
                {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                {saving ? "Saving..." : "Save Changes"}
              </Button>
            )}
            {dirty && <Badge variant="destructive" className="w-full justify-center">Unsaved Changes</Badge>}
            <Button variant="ghost" size="sm" className="w-full gap-2" onClick={toggleDarkMode}>
              {darkMode ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
              {darkMode ? "Light Mode" : "Dark Mode"}
            </Button>
          </div>
        </div>

        {/* Main */}
        <div className="flex-1 flex flex-col overflow-hidden">
          {/* Error banner */}
          {error && (
            <div className="bg-destructive/10 border-b border-destructive/20 px-4 py-2 flex items-center gap-2 text-sm text-destructive">
              <AlertTriangle className="h-4 w-4" />
              <span className="flex-1">{error}</span>
              <Button variant="ghost" size="sm" onClick={clearError}>Dismiss</Button>
            </div>
          )}

          {/* Loading overlay */}
          {saveLoading && (
            <div className="absolute inset-0 z-40 flex items-center justify-center bg-background/80">
              <div className="flex flex-col items-center gap-3">
                <Loader2 className="h-8 w-8 animate-spin text-primary" />
                <p className="text-sm text-muted-foreground">Loading save file...</p>
              </div>
            </div>
          )}

          <ScrollArea className="flex-1 w-full">
            <div className="p-6 min-h-full flex flex-col">{renderPage()}</div>
          </ScrollArea>
        </div>
      </div>
    </TooltipProvider>
  );
}
