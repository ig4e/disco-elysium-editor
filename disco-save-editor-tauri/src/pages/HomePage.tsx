import { useEffect } from "react";
import { useStore } from "@/store";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { FolderOpen, RefreshCw, Loader2 } from "lucide-react";

interface HomePageProps {
  onLoaded: () => void;
}

export default function HomePage({ onLoaded }: HomePageProps) {
  const { saves, savesLoading, discoverSaves, locateSaveFile, loadSave, currentSave } = useStore();

  useEffect(() => {
    discoverSaves();
  }, [discoverSaves]);

  useEffect(() => {
    if (currentSave) {
      onLoaded();
    }
  }, [currentSave, onLoaded]);

  const handleLoad = async (path: string) => {
    await loadSave(path);
  };

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Disco Elysium Save Editor</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Select a save file to begin editing
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={locateSaveFile} disabled={savesLoading}>
            <FolderOpen className="h-4 w-4 mr-2" />
            Browse
          </Button>
          <Button variant="outline" onClick={discoverSaves} disabled={savesLoading}>
            {savesLoading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <RefreshCw className="h-4 w-4 mr-2" />}
            Refresh
          </Button>
        </div>
      </div>

      {savesLoading && saves.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin mb-4" />
          <p className="text-sm">Scanning for save files...</p>
        </div>
      ) : saves.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-20 text-muted-foreground">
            <FolderOpen className="h-12 w-12 mb-4" />
            <p className="text-sm">No save files found</p>
            <p className="text-xs mt-1 mb-6 text-center">
              Make sure Disco Elysium saves exist in the default location or <br /> manually locate a save file (.zip or .ntwtf)
            </p>
            <Button onClick={locateSaveFile}>
              <FolderOpen className="h-4 w-4 mr-2" />
              Locate Save File
            </Button>
          </CardContent>
        </Card>
      ) : (
        <ScrollArea className="h-[calc(100vh-12rem)]">
          <div className="space-y-2">
            {saves.map((s) => (
              <Card
                key={s.path}
                className="cursor-pointer hover:bg-accent/50 transition-colors"
                onClick={() => handleLoad(s.path)}
              >
                <CardHeader className="py-3 px-4">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-sm font-medium">{s.name}</CardTitle>
                    <CardDescription className="text-xs">{s.last_modified}</CardDescription>
                  </div>
                  <CardDescription className="text-xs truncate">{s.path}</CardDescription>
                </CardHeader>
              </Card>
            ))}
          </div>
        </ScrollArea>
      )}
    </div>
  );
}
