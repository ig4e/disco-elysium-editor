import { useEffect, useState } from "react";
import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Search, Globe, MapPin, Cloud, Database, Info, Loader2, Sparkles, Wind, Navigation } from "lucide-react";

export default function WorldPage() {
  const { currentSave, updateField, luaVariables, luaLoading, loadLuaVariables, setLuaEdit, luaEdits } = useStore();
  const [luaFilter, setLuaFilter] = useState("");

  if (!currentSave) return null;

  const { location_flags, reputation, weather_preset, area_id } = currentSave;

  const updateLocation = (field: keyof typeof location_flags, value: boolean) => {
    updateField("location_flags", { ...location_flags, [field]: value });
  };

  const updateReputation = (field: keyof typeof reputation, value: number) => {
    updateField("reputation", { ...reputation, [field]: value });
  };

  const handleLuaSearch = () => {
    loadLuaVariables(luaFilter || undefined);
  };

  const reputationInfo = {
    communist: { label: "Mazovian Socio-Economics", color: "text-red-500", desc: "Left-wing revolutionary thought." },
    ultraliberal: { label: "Indirect Taxation", color: "text-yellow-500", desc: "Free market and hustle culture." },
    moralist: { label: "Kingdom of Conscience", color: "text-blue-500", desc: "The status quo and centrism." },
    nationalist: { label: "Revacholian Nationhood", color: "text-orange-500", desc: "Traditionalism and nationalism." },
    kim: { label: "Lieutenant Karat", color: "text-sky-400", desc: "Kim Kitsuragi's personal respect for you." },
  };

  return (
    <TooltipProvider>
      <div className="space-y-8 max-w-6xl mx-auto">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <h2 className="text-2xl font-bold flex items-center gap-2">
              <Globe className="h-6 w-6 text-primary" />
              World State & variables
            </h2>
            <p className="text-sm text-muted-foreground">Modify global flags, reputation, and raw Lua variables.</p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Area & Weather */}
          <Card className="md:col-span-1 shadow-md border-primary/10">
            <CardHeader className="pb-3 border-b bg-muted/20">
              <CardTitle className="text-sm font-bold flex items-center gap-2 uppercase tracking-wider">
                <Navigation className="h-4 w-4 text-primary" />
                Environment
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-6 space-y-5">
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                   <Label className="text-[10px] font-bold uppercase text-muted-foreground">Current Area ID</Label>
                   <MapPin className="h-3 w-3 text-primary opacity-50" />
                </div>
                <Input
                  value={area_id}
                  onChange={(e) => updateField("area_id", e.target.value)}
                  className="font-mono text-xs bg-muted/10 h-9"
                />
              </div>
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                   <Label className="text-[10px] font-bold uppercase text-muted-foreground">Weather Preset</Label>
                   <Cloud className="h-3 w-3 text-primary opacity-50" />
                </div>
                <div className="flex gap-2">
                   <Input
                    type="number"
                    value={weather_preset}
                    onChange={(e) => updateField("weather_preset", parseInt(e.target.value) || 0)}
                    className="font-mono text-xs bg-muted/10 h-9 flex-1"
                  />
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button variant="outline" size="icon" className="h-9 w-9">
                        <Info className="h-4 w-4 opacity-50" />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent className="text-[10px] max-w-[150px]">
                      0: Clear, 1: Rain, 2: Snow, etc. Presets depend on current area.
                    </TooltipContent>
                  </Tooltip>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Reputation */}
          <Card className="md:col-span-2 shadow-md border-primary/10">
            <CardHeader className="pb-3 border-b bg-muted/20">
              <CardTitle className="text-sm font-bold flex items-center gap-2 uppercase tracking-wider">
                <Sparkles className="h-4 w-4 text-primary" />
                Political & Personal Reputation
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-6 grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-4">
              {Object.entries(reputationInfo).map(([key, info]) => (
                <div key={key} className="group flex flex-col gap-1">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className={`text-[11px] font-bold ${info.color}`}>{info.label}</span>
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <Info className="h-3 w-3 opacity-30 group-hover:opacity-100 transition-opacity cursor-help" />
                        </TooltipTrigger>
                        <TooltipContent className="text-[10px]">{info.desc}</TooltipContent>
                      </Tooltip>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Input
                      type="number"
                      step="0.1"
                      value={reputation[key as keyof typeof reputation]}
                      onChange={(e) => updateReputation(key as any, parseFloat(e.target.value) || 0)}
                      className="font-mono text-xs h-8 bg-muted/5 border-primary/10 focus:border-primary/30"
                    />
                    <Badge variant="outline" className="h-8 px-2 text-[10px] min-w-[3rem] justify-center text-muted-foreground bg-background">
                       PTS
                    </Badge>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* Location Flags */}
          <Card className="md:col-span-3 shadow-sm border-dashed">
            <CardHeader className="py-3 px-4 flex flex-row items-center justify-between bg-muted/10 border-b border-dashed">
              <CardTitle className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground flex items-center gap-2">
                <Navigation className="h-3 w-3" />
                Discovery & Fast Travel Flags
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-4 pb-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                {[
                  { key: "was_church_visited" as const, label: "Church Visited" },
                  { key: "was_fishing_village_visited" as const, label: "Fishing Village" },
                  { key: "was_quicktravel_church_discovered" as const, label: "FT: Church" },
                  { key: "was_quicktravel_fishing_village_discovered" as const, label: "FT: Fishing Village" },
                ].map((f) => (
                  <div key={f.key} className="flex items-center justify-between bg-muted/5 p-2 rounded-lg border border-primary/5">
                    <Label className="text-[11px] font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
                      {f.label}
                    </Label>
                    <Switch
                      checked={location_flags[f.key]}
                      onCheckedChange={(v) => updateLocation(f.key, v)}
                      className="scale-75"
                    />
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        <Separator className="opacity-50" />

        {/* Lua Variables */}
        <Card className="shadow-lg border-primary/20">
          <CardHeader className="bg-muted/30 border-b">
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <CardTitle className="text-base font-bold flex items-center gap-2 uppercase">
                  <Database className="h-5 w-5 text-primary" />
                  Global Variable Search (LUA)
                </CardTitle>
                <CardDescription className="text-xs">
                  Direct access to the game's internal Lua variable store. Use with caution.
                </CardDescription>
              </div>
              <Badge variant="secondary" className="font-mono text-[10px] px-2 py-1">
                {currentSave.lua_variable_count} LOADED
              </Badge>
            </div>
          </CardHeader>
          <CardContent className="pt-6 space-y-6">
            <div className="flex gap-3">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground opacity-50" />
                <Input
                  placeholder="Filter by name (e.g. 'thought', 'money', 'kim_trust')..."
                  value={luaFilter}
                  onChange={(e) => setLuaFilter(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && handleLuaSearch()}
                  className="pl-10 h-10 bg-muted/20 border-primary/10"
                />
              </div>
              <Button 
                onClick={handleLuaSearch} 
                className="h-10 px-6 font-bold"
                disabled={luaLoading}
              >
                {luaLoading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Search className="h-4 w-4 mr-2" />}
                {luaLoading ? "SCANNING..." : "SEARCH ENGINE"}
              </Button>
            </div>

            {luaVariables.length > 0 ? (
              <div className="rounded-md border bg-muted/10">
                <ScrollArea className="h-[400px]">
                  <div className="p-1">
                    {luaVariables.map((v) => (
                      <div
                        key={v.key}
                        className="group flex items-center gap-4 p-2 rounded hover:bg-background/80 transition-colors border-b last:border-0 border-primary/5"
                      >
                        <Badge 
                          variant="outline" 
                          className={`text-[9px] font-mono w-14 justify-center py-0.5 ${
                            v.var_type === 'number' ? 'border-blue-500/30 text-blue-500' : 
                            v.var_type === 'boolean' ? 'border-green-500/30 text-green-500' : 'border-amber-500/30 text-amber-500'
                          }`}
                        >
                          {v.var_type.toUpperCase()}
                        </Badge>
                        <span className="font-mono text-[11px] flex-1 truncate text-foreground/80 group-hover:text-primary transition-colors">
                          {v.key}
                        </span>
                        <div className="flex items-center gap-2">
                           <Input
                            value={luaEdits[v.key] ?? v.value}
                            onChange={(e) => setLuaEdit(v.key, e.target.value)}
                            className="w-48 h-7 text-[11px] font-mono bg-background border-primary/10 focus:border-primary/40 focus:ring-0"
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </ScrollArea>
              </div>
            ) : luaLoading ? (
              <div className="h-[200px] flex flex-col items-center justify-center border-2 border-dashed rounded-xl opacity-50">
                <Loader2 className="h-8 w-8 animate-spin text-primary mb-2" />
                <p className="text-xs uppercase tracking-widest font-bold">Accessing Database...</p>
              </div>
            ) : (
              <div className="h-[100px] flex items-center justify-center border-2 border-dashed rounded-xl opacity-40">
                <p className="text-xs uppercase tracking-widest font-bold">Search to view variables</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </TooltipProvider>
  );
}
