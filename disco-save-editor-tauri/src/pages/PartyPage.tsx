import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Separator } from "@/components/ui/separator";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Users, User, Tv, Shield, Info } from "lucide-react";

export default function PartyPage() {
  const { currentSave, updateField } = useStore();
  if (!currentSave) return null;

  const { party_state, hud_state, game_mode } = currentSave;

  const updateParty = (field: keyof typeof party_state, value: boolean | number) => {
    updateField("party_state", { ...party_state, [field]: value });
  };

  const updateHud = (field: keyof typeof hud_state, value: boolean) => {
    updateField("hud_state", { ...hud_state, [field]: value });
  };

  return (
    <TooltipProvider>
      <div className="space-y-6 max-w-5xl mx-auto">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold flex items-center gap-2">
            <Users className="h-6 w-6 text-primary" />
            Party & HUD
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Kim Kitsuragi */}
          <Card className="border-primary/20 bg-primary/5">
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-primary">
                <User className="h-5 w-5" />
                Kim Kitsuragi
              </CardTitle>
              <CardDescription>Manage the Lieutenant's state and availability</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="grid grid-cols-2 gap-x-8 gap-y-3">
                {[
                  { key: "is_kim_in_party" as const, label: "In Party", tooltip: "Whether Kim is following you" },
                  { key: "is_kim_left_outside" as const, label: "Left Outside", tooltip: "Kim is waiting outside a building" },
                  { key: "is_kim_abandoned" as const, label: "Abandoned", tooltip: "You've sent Kim away or he left" },
                  { key: "is_kim_away_up_to_morning" as const, label: "Away Until Morning", tooltip: "Kim is sleeping at the Whirling" },
                  { key: "is_kim_sleeping_in_his_room" as const, label: "Sleeping in Room", tooltip: "Kim is currently in his bedroom" },
                  { key: "is_kim_saying_good_morning" as const, label: "Saying Good Morning", tooltip: "Dialogue flag for morning greeting" },
                ].map((f) => (
                  <div key={f.key} className="flex items-center justify-between group">
                    <div className="flex items-center gap-1.5">
                      <Label className="text-sm cursor-help">{f.label}</Label>
                      <Tooltip>
                        <TooltipTrigger>
                          <Info className="h-3.5 w-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
                        </TooltipTrigger>
                        <TooltipContent>{f.tooltip}</TooltipContent>
                      </Tooltip>
                    </div>
                    <Switch
                      checked={party_state[f.key] as boolean}
                      onCheckedChange={(v) => updateParty(f.key, v)}
                      className="scale-90"
                    />
                  </div>
                ))}
              </div>
              <Separator className="my-2 bg-primary/10" />
              <div className="grid grid-cols-2 gap-4 pt-1">
                <div className="space-y-1.5">
                  <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Time Since Sleeping</Label>
                  <Input
                    type="number"
                    value={party_state.time_since_kim_went_sleeping}
                    onChange={(e) => updateParty("time_since_kim_went_sleeping", parseInt(e.target.value) || 0)}
                    className="h-8 bg-background"
                  />
                </div>
                <div className="space-y-1.5">
                  <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Arrival Location ID</Label>
                  <Input
                    type="number"
                    value={party_state.kim_last_arrival_location}
                    onChange={(e) => updateParty("kim_last_arrival_location", parseInt(e.target.value) || 0)}
                    className="h-8 bg-background"
                  />
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Cuno */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-orange-400">
                <User className="h-5 w-5" />
                Cuno
              </CardTitle>
              <CardDescription>Manage Cuno's role in the investigation</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="space-y-3">
                {[
                  { key: "is_cuno_in_party" as const, label: "In Party", tooltip: "Cuno is your temporary partner" },
                  { key: "is_cuno_left_outside" as const, label: "Left Outside", tooltip: "Cuno is waiting" },
                  { key: "is_cuno_abandoned" as const, label: "Abandoned", tooltip: "Cuno is back at the Shack" },
                ].map((f) => (
                  <div key={f.key} className="flex items-center justify-between group">
                    <div className="flex items-center gap-1.5">
                      <Label className="text-sm cursor-help">{f.label}</Label>
                      <Tooltip>
                        <TooltipTrigger>
                          <Info className="h-3.5 w-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
                        </TooltipTrigger>
                        <TooltipContent>{f.tooltip}</TooltipContent>
                      </Tooltip>
                    </div>
                    <Switch
                      checked={party_state[f.key] as boolean}
                      onCheckedChange={(v) => updateParty(f.key, v)}
                    />
                  </div>
                ))}
              </div>
              <Separator className="my-2" />
              <div className="grid grid-cols-2 gap-4 pt-1">
                <div className="space-y-1.5">
                  <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Wait Location ID</Label>
                  <Input
                    type="number"
                    value={party_state.cuno_wait_location}
                    onChange={(e) => updateParty("cuno_wait_location", parseInt(e.target.value) || 0)}
                    className="h-8"
                  />
                </div>
                <div className="space-y-1.5">
                  <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Arrival Location ID</Label>
                  <Input
                    type="number"
                    value={party_state.cuno_last_arrival_location}
                    onChange={(e) => updateParty("cuno_last_arrival_location", parseInt(e.target.value) || 0)}
                    className="h-8"
                  />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* World & Mode */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5" />
                World & Rules
              </CardTitle>
              <CardDescription>Global state and difficulty settings</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between group">
                <div className="flex items-center gap-1.5">
                  <Label className="text-sm font-semibold">Has Hangover</Label>
                  <Tooltip>
                    <TooltipTrigger>
                      <Info className="h-3.5 w-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
                    </TooltipTrigger>
                    <TooltipContent>Applies the Day 1 penalty</TooltipContent>
                  </Tooltip>
                </div>
                <Switch
                  checked={party_state.has_hangover}
                  onCheckedChange={(v) => updateParty("has_hangover", v)}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Sleep Location ID</Label>
                  <Input
                    type="number"
                    value={party_state.sleep_location}
                    onChange={(e) => updateParty("sleep_location", parseInt(e.target.value) || 0)}
                    className="h-8"
                  />
                </div>
                <div className="space-y-1.5">
                  <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Wait Location ID</Label>
                  <Input
                    type="number"
                    value={party_state.wait_location}
                    onChange={(e) => updateParty("wait_location", parseInt(e.target.value) || 0)}
                    className="h-8"
                  />
                </div>
              </div>

              <Separator />

              <div className="space-y-2">
                <Label className="text-sm font-semibold">Game Mode</Label>
                <Select
                  value={game_mode}
                  onValueChange={(v) => updateField("game_mode", v)}
                >
                  <SelectTrigger className="h-9">
                    <SelectValue placeholder="Select mode" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="NORMAL">Normal</SelectItem>
                    <SelectItem value="HARDCORE">Hardcore</SelectItem>
                  </SelectContent>
                </Select>
                <p className="text-[11px] text-muted-foreground">
                  Switching between Normal and Hardcore affects skill check difficulties and resource gains.
                </p>
              </div>
            </CardContent>
          </Card>

          {/* HUD & Interface */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2">
                <Tv className="h-5 w-5" />
                HUD & Portrait
              </CardTitle>
              <CardDescription>Interface notifications and portrait state</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-x-8 gap-y-3">
                {[
                  { key: "portrait_obscured" as const, label: "Portr. Obscured", tooltip: "Initial shadowy portrait" },
                  { key: "portrait_shaved" as const, label: "Portr. Shaved", tooltip: "Harry without the mutton chops" },
                  { key: "portrait_fascist" as const, label: "Portr. Fascist", tooltip: "Serious expression from fascist quest" },
                  { key: "portrait_expression_stopped" as const, label: "Stop Expression", tooltip: "Stop 'The Expression'" },
                  { key: "charsheet_notification" as const, label: "Char Sheet Notif", tooltip: "Show red dot on Character Sheet icon" },
                  { key: "inventory_notification" as const, label: "Inventory Notif", tooltip: "Show red dot on Inventory icon" },
                  { key: "journal_notification" as const, label: "Journal Notif", tooltip: "Show red dot on Journal icon" },
                  { key: "thc_notification" as const, label: "Thought Cab Notif", tooltip: "Show red dot on Thought Cabinet icon" },
                ].map((f) => (
                  <div key={f.key} className="flex items-center justify-between group">
                    <Label className="text-xs">{f.label}</Label>
                    <Switch
                      checked={hud_state[f.key]}
                      onCheckedChange={(v) => updateHud(f.key, v)}
                      className="scale-75 origin-right"
                    />
                  </div>
                ))}
              </div>
              <Separator />
              <div className="space-y-2">
                <Label className="text-[11px] uppercase tracking-wider text-muted-foreground">Active Item Type Notifications</Label>
                <div className="grid grid-cols-2 gap-2">
                  {[
                    { key: "inv_clothes_notification" as const, label: "Clothing" },
                    { key: "inv_tools_notification" as const, label: "Tools" },
                    { key: "inv_reading_notification" as const, label: "Reading" },
                    { key: "inv_pawnables_notification" as const, label: "Pawnables" },
                  ].map((f) => (
                    <div key={f.key} className="flex items-center gap-2 bg-muted/50 p-1.5 rounded-sm px-2">
                      <Switch
                        checked={hud_state[f.key]}
                        onCheckedChange={(v) => updateHud(f.key, v)}
                        className="scale-75"
                      />
                      <span className="text-[10px] font-medium">{f.label}</span>
                    </div>
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </TooltipProvider>
  );
}

