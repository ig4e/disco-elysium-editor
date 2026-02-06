import { useState } from "react";
import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Search, Brain, Clock, Zap, CheckCircle2, XCircle, Info } from "lucide-react";
import type { ThoughtDisplay } from "@/types";

const STATE_ORDER = ["NotAcquired", "Gained", "Processing", "Internalized", "Forgotten"] as const;
const STATE_COLORS: Record<string, string> = {
  NotAcquired: "bg-slate-500/10 text-slate-400 border-slate-500/20",
  Gained: "bg-blue-500/10 text-blue-400 border-blue-500/20",
  Processing: "bg-yellow-500/10 text-yellow-400 border-yellow-500/20",
  Internalized: "bg-green-500/10 text-green-400 border-green-500/20",
  Forgotten: "bg-red-500/10 text-red-400 border-red-500/20",
};

export default function ThoughtCabinetPage() {
  const { currentSave, updateField } = useStore();
  const [search, setSearch] = useState("");
  const [filterState, setFilterState] = useState<string>("all");

  if (!currentSave) return null;

  const cycleState = (idx: number) => {
    const updated = [...currentSave.thoughts];
    const current = updated[idx].state;
    const currentIdx = STATE_ORDER.indexOf(current as typeof STATE_ORDER[number]);
    const nextIdx = (currentIdx + 1) % STATE_ORDER.length;
    updated[idx] = { ...updated[idx], state: STATE_ORDER[nextIdx] };
    updateField("thoughts", updated);
  };

  const updateTimeLeft = (idx: number, val: number) => {
    const updated = [...currentSave.thoughts];
    updated[idx] = { ...updated[idx], time_left: val };
    updateField("thoughts", updated);
  };

  const internalizeAll = () => {
    const updated = currentSave.thoughts.map((t) => ({ ...t, state: "Internalized", time_left: 0 }));
    updateField("thoughts", updated);
  };

  const clearAll = () => {
    const updated = currentSave.thoughts.map((t) => ({ ...t, state: "NotAcquired", time_left: 0 }));
    updateField("thoughts", updated);
  };

  const filtered = currentSave.thoughts.filter((t) => {
    if (filterState !== "all" && t.state !== filterState) return false;
    if (search) {
      const q = search.toLowerCase();
      return t.display_name.toLowerCase().includes(q) || t.name.toLowerCase().includes(q);
    }
    return true;
  });

  const counts = {
    NotAcquired: currentSave.thoughts.filter((t) => t.state === "NotAcquired").length,
    Processing: currentSave.thoughts.filter((t) => t.state === "Processing").length,
    Internalized: currentSave.thoughts.filter((t) => t.state === "Internalized").length,
  };

  return (
    <TooltipProvider>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold flex items-center gap-2">
            <Brain className="h-6 w-6 text-primary" />
            Thought Cabinet
          </h2>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={internalizeAll} className="gap-2">
              <CheckCircle2 className="h-4 w-4 text-green-500" />
              Internalize All
            </Button>
            <Button variant="outline" size="sm" onClick={clearAll} className="gap-2">
              <XCircle className="h-4 w-4 text-red-500" />
              Clear All
            </Button>
          </div>
        </div>

        <div className="flex items-center gap-4 bg-muted/30 p-3 rounded-lg border">
          <div className="flex items-center gap-2 px-3 border-r">
            <span className="text-xs font-semibold uppercase text-muted-foreground tracking-wider">Stats:</span>
          </div>
          <Badge variant="outline" className={STATE_COLORS.NotAcquired}>{counts.NotAcquired} Available</Badge>
          <Badge variant="outline" className={STATE_COLORS.Processing}>{counts.Processing} Researching</Badge>
          <Badge variant="outline" className={STATE_COLORS.Internalized}>{counts.Internalized} Internalized</Badge>
        </div>

        <div className="flex flex-col sm:flex-row gap-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search thoughts, bonuses or descriptions..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9 h-10"
            />
          </div>
          <Tabs value={filterState} onValueChange={setFilterState} className="w-auto">
            <TabsList className="h-10">
              <TabsTrigger value="all">All</TabsTrigger>
              <TabsTrigger value="Processing">Researching</TabsTrigger>
              <TabsTrigger value="Internalized">Internalized</TabsTrigger>
              <TabsTrigger value="Gained">Gained</TabsTrigger>
              <TabsTrigger value="Forgotten">Forgotten</TabsTrigger>
            </TabsList>
          </Tabs>
        </div>

        <ScrollArea className="h-[calc(100vh-20rem)] rounded-md">
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4 pb-6">
            {filtered.map((t) => {
              const idx = currentSave.thoughts.indexOf(t);
              const isInternalized = t.state === "Internalized";
              const isProcessing = t.state === "Processing";

              return (
                <Card key={t.name} className={`overflow-hidden transition-all border-l-4 ${isInternalized ? "border-l-green-500" : isProcessing ? "border-l-yellow-500" : "border-l-muted"}`}>
                  <CardHeader className="py-4 px-5">
                    <div className="flex items-start justify-between gap-4">
                      <div className="space-y-1">
                        <CardTitle className="text-base font-bold tracking-tight">{t.display_name}</CardTitle>
                        <div className="flex gap-2 items-center">
                          {t.thought_type && (
                            <Badge variant="secondary" className="text-[10px] uppercase font-bold py-0 h-4">{t.thought_type}</Badge>
                          )}
                          {t.is_cursed && (
                            <Badge variant="destructive" className="text-[10px] uppercase font-bold py-0 h-4">Cursed</Badge>
                          )}
                        </div>
                      </div>
                      <Badge
                        variant="outline"
                        className={`cursor-pointer px-3 py-1 hover:brightness-110 select-none ${STATE_COLORS[t.state] || ""}`}
                        onClick={() => cycleState(idx)}
                      >
                        {t.state}
                      </Badge>
                    </div>
                  </CardHeader>
                  <CardContent className="px-5 pb-5 space-y-4">
                    <div className="space-y-2">
                       <p className="text-sm leading-relaxed text-foreground/90 italic font-serif">
                        {isInternalized && t.completion_description ? t.completion_description : t.description}
                      </p>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
                      <div className="space-y-2 p-3 bg-muted/50 rounded-lg border border-border/50">
                        <div className="flex items-center gap-2 text-yellow-500 mb-1">
                          <Clock className="h-3.5 w-3.5" />
                          <span className="text-[11px] font-bold uppercase tracking-wider">While Researching</span>
                        </div>
                        <p className="text-xs font-medium leading-snug">
                          {t.bonus_while_processing || "No research bonus"}
                        </p>
                        {isProcessing && (
                          <div className="flex items-center gap-2 mt-2 pt-2 border-t border-yellow-500/20">
                            <span className="text-[10px] text-muted-foreground font-bold uppercase">Mins left:</span>
                            <Input
                              type="number"
                              value={t.time_left}
                              onChange={(e) => updateTimeLeft(idx, parseInt(e.target.value) || 0)}
                              className="w-16 h-7 text-xs bg-background border-yellow-500/30"
                            />
                          </div>
                        )}
                      </div>

                      <div className={`space-y-2 p-3 rounded-lg border border-border/50 ${isInternalized ? "bg-green-500/5 border-green-500/20" : "bg-muted/50"}`}>
                        <div className={`flex items-center gap-2 mb-1 ${isInternalized ? "text-green-500" : "text-muted-foreground"}`}>
                          <Zap className="h-3.5 w-3.5" />
                          <span className="text-[11px] font-bold uppercase tracking-wider">After Internalization</span>
                        </div>
                        <p className="text-xs font-medium leading-snug">
                          {t.bonus_when_completed || "No final bonus"}
                        </p>
                      </div>
                    </div>

                    {t.requirement && (
                      <div className="text-[10px] text-muted-foreground flex items-center gap-1.5 pt-1">
                        <Info className="h-3 w-3" />
                        <span>Source: {t.requirement}</span>
                      </div>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </ScrollArea>
      </div>
    </TooltipProvider>
  );
}

