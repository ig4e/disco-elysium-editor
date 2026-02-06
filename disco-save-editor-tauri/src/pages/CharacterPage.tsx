import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Switch } from "@/components/ui/switch";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { User, Coins, TrendingUp, Heart, Brain, Star, Info, Clock } from "lucide-react";
import type { AbilityDisplay, SkillDisplay } from "@/types";

const ABILITY_COLORS: Record<string, string> = {
  INT: "bg-blue-500/20 text-blue-400 border-blue-500/30",
  PSY: "bg-purple-500/20 text-purple-400 border-purple-500/30",
  FYS: "bg-red-500/20 text-red-400 border-red-500/30",
  MOT: "bg-green-500/20 text-green-400 border-green-500/30",
};

export default function CharacterPage() {
  const { currentSave, updateField } = useStore();
  if (!currentSave) return null;

  const handleNumberField = (key: keyof typeof currentSave, val: string) => {
    const n = parseInt(val, 10);
    if (!isNaN(n)) updateField(key, n as never);
  };

  const updateAbility = (idx: number, field: keyof AbilityDisplay, value: number | boolean) => {
    const updated = [...currentSave.abilities];
    updated[idx] = { ...updated[idx], [field]: value };
    updateField("abilities", updated);
  };

  const updateSkill = (idx: number, field: keyof SkillDisplay, value: number | boolean) => {
    const updated = [...currentSave.skills];
    updated[idx] = { ...updated[idx], [field]: value };
    updateField("skills", updated);
  };

  const groupedSkills = currentSave.skills.reduce<Record<string, SkillDisplay[]>>((acc, s) => {
    const group = s.ability_type || "Other";
    if (!acc[group]) acc[group] = [];
    acc[group].push(s);
    return acc;
  }, {});

  return (
    <TooltipProvider>
      <div className="space-y-6 max-w-6xl mx-auto h-full">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold flex items-center gap-2">
            <User className="h-6 w-6 text-primary" />
            Character Sheet
          </h2>
        </div>

        {/* Resources Cards */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {[
            { label: "Experience", key: "xp_amount" as const, icon: <TrendingUp className="h-4 w-4" />, suffix: "XP" },
            { label: "Level", key: "level" as const, icon: <Star className="h-4 w-4" />, suffix: "" },
            { label: "Skill Points", key: "skill_points" as const, icon: <Brain className="h-4 w-4" />, suffix: "PTS" },
            { label: "Money", key: "money" as const, icon: <Coins className="h-4 w-4" />, suffix: "CENTS" },
          ].map((f) => (
            <Card key={f.key} className="bg-primary/5 border-primary/10">
              <CardContent className="p-4">
                <div className="flex items-center gap-2 mb-2 text-primary">
                  {f.icon}
                  <Label className="text-[10px] font-bold uppercase tracking-widest">{f.label}</Label>
                </div>
                <div className="flex items-center gap-2">
                  <Input
                    type="number"
                    value={currentSave[f.key] as number}
                    onChange={(e) => handleNumberField(f.key, e.target.value)}
                    className="h-9 font-mono text-lg bg-background"
                  />
                  <span className="text-[10px] font-bold text-muted-foreground">{f.suffix}</span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {[
            { label: "Health / Endurance", key: "health" as const, icon: <Heart className="h-4 w-4 text-red-500" /> },
            { label: "Morale / Volition", key: "morale" as const, icon: <Brain className="h-4 w-4 text-purple-500" /> },
            { label: "Current Day", key: "day" as const, icon: <Clock className="h-4 w-4" /> },
            { label: "Current Hours", key: "hours" as const, icon: <Clock className="h-4 w-4" /> },
          ].map((f) => (
            <Card key={f.key}>
              <CardContent className="p-4">
                <div className="flex items-center gap-2 mb-2">
                  {f.icon}
                  <Label className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">{f.label}</Label>
                </div>
                <Input
                  type="number"
                  value={currentSave[f.key] as number}
                  onChange={(e) => handleNumberField(f.key, e.target.value)}
                  className="h-9 font-mono text-lg"
                />
              </CardContent>
            </Card>
          ))}
        </div>

        <Tabs defaultValue="abilities" className="w-full">
          <TabsList className="grid w-full grid-cols-2 max-w-[400px]">
            <TabsTrigger value="abilities">Abilities</TabsTrigger>
            <TabsTrigger value="skills">Physical & Mental Skills</TabsTrigger>
          </TabsList>

          <TabsContent value="abilities" className="pt-4 space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {currentSave.abilities.map((a, idx) => (
                <Card key={a.save_key} className="overflow-hidden border-l-4 border-l-primary">
                  <CardHeader className="py-4 px-5 bg-muted/30">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <Badge className={ABILITY_COLORS[a.type_code] || ""}>{a.type_code}</Badge>
                        <CardTitle className="text-base">{a.display_name}</CardTitle>
                      </div>
                      <div className="flex items-center gap-3 bg-background/50 px-2 py-1 rounded-md border text-xs">
                        <Label className="text-xs font-semibold">Signature</Label>
                        <Switch
                          checked={a.is_signature}
                          onCheckedChange={(v) => updateAbility(idx, "is_signature", v)}
                          className="scale-75"
                        />
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="px-5 py-4">
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-1.5">
                        <Label className="text-[10px] font-bold uppercase text-muted-foreground">Current Value</Label>
                        <Input
                          type="number"
                          value={a.value}
                          onChange={(e) => updateAbility(idx, "value", parseInt(e.target.value) || 0)}
                          className="h-10 text-lg font-mono"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <Label className="text-[10px] font-bold uppercase text-muted-foreground">Learning Cap (Max)</Label>
                        <Input
                          type="number"
                          value={a.maximum_value}
                          onChange={(e) => updateAbility(idx, "maximum_value", parseInt(e.target.value) || 0)}
                          className="h-10 text-lg font-mono"
                        />
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </TabsContent>

          <TabsContent value="skills" className="pt-4">
            <div className="space-y-8">
              {Object.entries(groupedSkills).map(([group, skills]) => (
                <div key={group} className="space-y-4">
                  <div className="flex items-center gap-3 px-1">
                    <Badge className={`${ABILITY_COLORS[group] || ""} px-3 py-1 text-xs`}>{group}</Badge>
                    <Separator className="flex-1" />
                    <span className="text-xs font-bold text-muted-foreground uppercase tracking-widest">{group} GOVERNED SKILLS</span>
                  </div>
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                    {skills.map((s) => {
                      const idx = currentSave.skills.indexOf(s);
                      return (
                        <Card key={s.save_key} className="text-sm border-muted-foreground/10 hover:border-primary/20 transition-colors">
                          <CardHeader className="py-3 px-4 flex-row items-center justify-between space-y-0">
                            <CardTitle className="text-xs font-bold uppercase tracking-tight flex items-center gap-2">
                              {s.display_name}
                              {s.is_signature && <Badge variant="secondary" className="text-[8px] h-3.5 px-1 bg-yellow-500/10 text-yellow-500 border-yellow-500/20">SIG</Badge>}
                            </CardTitle>
                            {s.modifier_count > 0 && (
                                <Tooltip>
                                  <TooltipTrigger>
                                    <Badge variant="outline" className="text-[9px] h-4 bg-primary/5">{s.modifier_count} MODS</Badge>
                                  </TooltipTrigger>
                                  <TooltipContent>This skill has active modifiers in the save file</TooltipContent>
                                </Tooltip>
                            )}
                          </CardHeader>
                          <CardContent className="px-4 pb-3 space-y-3">
                            <div className="grid grid-cols-2 gap-3">
                              <div className="space-y-1">
                                <Label className="text-[9px] uppercase text-muted-foreground">Invested</Label>
                                <Input
                                  type="number"
                                  value={s.rank_value}
                                  onChange={(e) => updateSkill(idx, "rank_value", parseInt(e.target.value) || 0)}
                                  className="h-7 text-xs font-mono"
                                />
                              </div>
                              <div className="space-y-1">
                                <Label className="text-[9px] uppercase text-muted-foreground">Total</Label>
                                <Input
                                  type="number"
                                  value={s.value}
                                  onChange={(e) => updateSkill(idx, "value", parseInt(e.target.value) || 0)}
                                  className="h-7 text-xs font-mono bg-muted/30"
                                />
                              </div>
                            </div>
                            <div className="flex items-center justify-between pt-1">
                              <div className="flex items-center gap-2">
                                <Label className="text-[10px]">Can level up</Label>
                                <Switch
                                  checked={s.has_advancement}
                                  onCheckedChange={(v) => updateSkill(idx, "has_advancement", v)}
                                  className="scale-75"
                                />
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </TooltipProvider>
  );
}
