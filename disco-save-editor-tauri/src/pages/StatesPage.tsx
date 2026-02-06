import { useState } from "react";
import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Search, DoorOpen, Map, Circle, Info, Hash, Lock, Unlock, Eye, Sparkles } from "lucide-react";

export default function StatesPage() {
  const { currentSave, updateField } = useStore();
  const [doorSearch, setDoorSearch] = useState("");
  const [areaSearch, setAreaSearch] = useState("");
  const [orbSearch, setOrbSearch] = useState("");

  if (!currentSave) return null;

  const { door_states, area_states, shown_orbs } = currentSave;

  const doorEntries = Object.entries(door_states).filter(([k]) =>
    k.toLowerCase().includes(doorSearch.toLowerCase())
  );

  const areaEntries = Object.entries(area_states).filter(([k]) =>
    k.toLowerCase().includes(areaSearch.toLowerCase())
  );

  const orbEntries = Object.entries(shown_orbs).filter(([k]) =>
    k.toLowerCase().includes(orbSearch.toLowerCase())
  );

  const toggleDoor = (key: string) => {
    updateField("door_states", { ...door_states, [key]: !door_states[key] });
  };

  const updateAreaState = (key: string, value: number) => {
    updateField("area_states", { ...area_states, [key]: value });
  };

  const updateOrbState = (key: string, value: number) => {
    updateField("shown_orbs", { ...shown_orbs, [key]: value });
  };

  return (
    <TooltipProvider>
      <div className="space-y-6 max-w-6xl mx-auto h-full">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <h2 className="text-2xl font-bold flex items-center gap-2">
              <Sparkles className="h-6 w-6 text-primary" />
              World Object States
            </h2>
            <p className="text-sm text-muted-foreground">Manage doors, area reveal states, and thought orbs.</p>
          </div>
        </div>

        <Tabs defaultValue="doors" className="w-full">
          <TabsList className="grid grid-cols-3 max-w-[500px] mb-6">
            <TabsTrigger value="doors" className="flex items-center gap-2">
              <DoorOpen className="h-4 w-4" />
              Doors
              <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">{Object.keys(door_states).length}</Badge>
            </TabsTrigger>
            <TabsTrigger value="areas" className="flex items-center gap-2">
              <Map className="h-4 w-4" />
              Areas
              <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">{Object.keys(area_states).length}</Badge>
            </TabsTrigger>
            <TabsTrigger value="orbs" className="flex items-center gap-2">
              <Circle className="h-4 w-4" />
              Orbs
              <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">{Object.keys(shown_orbs).length}</Badge>
            </TabsTrigger>
          </TabsList>

          <TabsContent value="doors" className="space-y-4 outline-none">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search door identifiers (e.g. 'whirling', 'church')..."
                value={doorSearch}
                onChange={(e) => setDoorSearch(e.target.value)}
                className="pl-9 h-11 bg-muted/20"
              />
            </div>
            
            <ScrollArea className="h-[calc(100vh-320px)] pr-4">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 pb-8">
                {doorEntries.map(([key, value]) => (
                  <Card key={key} className="hover:border-primary/20 transition-colors">
                    <CardHeader className="py-3 px-4 flex flex-row items-center justify-between space-y-0 bg-muted/10 border-b">
                      <div className="flex items-center gap-2 min-w-0">
                        {value ? <Unlock className="h-3 w-3 text-green-500" /> : <Lock className="h-3 w-3 text-muted-foreground" />}
                        <CardTitle className="text-[10px] font-mono truncate text-muted-foreground">
                          {key}
                        </CardTitle>
                      </div>
                      <Switch 
                        checked={value} 
                        onCheckedChange={() => toggleDoor(key)}
                        className="scale-75"
                      />
                    </CardHeader>
                    <CardContent className="px-4 py-3 flex items-center justify-between">
                      <span className="text-xs font-bold uppercase tracking-tight">
                        {value ? "UNLOCKED / OPEN" : "LOCKED / CLOSED"}
                      </span>
                      <Badge variant={value ? "default" : "outline"} className="text-[9px] h-4">
                        {value ? "STATE_TRUE" : "STATE_FALSE"}
                      </Badge>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="areas" className="space-y-4 outline-none">
             <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search area state variables..."
                value={areaSearch}
                onChange={(e) => setAreaSearch(e.target.value)}
                className="pl-9 h-11 bg-muted/20"
              />
            </div>

            <ScrollArea className="h-[calc(100vh-320px)] pr-4">
               <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 pb-8">
                {areaEntries.map(([key, value]) => (
                  <Card key={key} className="hover:border-primary/20 transition-colors">
                    <CardHeader className="py-2 px-4 flex flex-row items-center justify-between space-y-0 bg-muted/5 border-b border-dashed">
                      <CardTitle className="text-[10px] font-mono truncate text-muted-foreground">
                        {key}
                      </CardTitle>
                      <Hash className="h-3 w-3 opacity-20" />
                    </CardHeader>
                    <CardContent className="px-4 py-3 flex items-center justify-between gap-4">
                       <span className="text-[10px] font-bold uppercase text-muted-foreground">Reveal Intensity</span>
                       <Input
                        type="number"
                        value={value}
                        onChange={(e) => updateAreaState(key, parseInt(e.target.value) || 0)}
                        className="h-7 w-20 text-[11px] font-mono bg-background"
                      />
                    </CardContent>
                  </Card>
                ))}
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="orbs" className="space-y-4 outline-none">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search thought orb identifiers..."
                value={orbSearch}
                onChange={(e) => setOrbSearch(e.target.value)}
                className="pl-9 h-11 bg-muted/20"
              />
            </div>

            <ScrollArea className="h-[calc(100vh-320px)] pr-4">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 pb-8">
                {orbEntries.map(([key, value]) => (
                  <Card key={key} className="hover:border-primary/20 transition-colors border-dashed">
                    <CardHeader className="py-2 px-4 flex flex-row items-center justify-between space-y-0 bg-muted/10 border-b">
                      <CardTitle className="text-[10px] font-mono truncate text-muted-foreground">
                        {key}
                      </CardTitle>
                      <Eye className="h-3 w-3 text-primary opacity-40" />
                    </CardHeader>
                    <CardContent className="px-4 py-3 flex items-center justify-between gap-4">
                       <div className="flex flex-col">
                         <span className="text-[10px] font-bold uppercase text-muted-foreground">Shown Count</span>
                         <span className="text-[9px] text-muted-foreground italic">Value {'>'} 0 means discovered</span>
                       </div>
                       <Input
                        type="number"
                        value={value}
                        onChange={(e) => updateOrbState(key, parseInt(e.target.value) || 0)}
                        className="h-7 w-20 text-[11px] font-mono bg-background"
                      />
                    </CardContent>
                  </Card>
                ))}
              </div>
            </ScrollArea>
          </TabsContent>
        </Tabs>
      </div>
    </TooltipProvider>
  );
}
