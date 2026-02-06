import { useState } from "react";
import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Search, RotateCcw } from "lucide-react";

export default function WhiteChecksPage() {
  const {
    currentSave, resetCheckKeys, resetSeenCheckKeys,
    addResetCheck, removeResetCheck, addResetSeenCheck, removeResetSeenCheck
  } = useStore();
  const [search, setSearch] = useState("");

  if (!currentSave) return null;

  const filterChecks = (checks: typeof currentSave.failed_checks) =>
    checks.filter(
      (c) =>
        c.key.toLowerCase().includes(search.toLowerCase()) ||
        c.skill_display_name.toLowerCase().includes(search.toLowerCase()) ||
        c.flag_name.toLowerCase().includes(search.toLowerCase())
    );

  const selectAllFailed = () => {
    currentSave.failed_checks.forEach((c) => {
      if (!resetCheckKeys.includes(c.key)) addResetCheck(c.key);
    });
  };

  const clearAllFailed = () => {
    resetCheckKeys.forEach((k) => removeResetCheck(k));
  };

  const selectAllSeen = () => {
    currentSave.seen_checks.forEach((c) => {
      if (!resetSeenCheckKeys.includes(c.key)) addResetSeenCheck(c.key);
    });
  };

  const clearAllSeen = () => {
    resetSeenCheckKeys.forEach((k) => removeResetSeenCheck(k));
  };

  const renderCheck = (check: typeof currentSave.failed_checks[0], isSeen: boolean) => {
    const isSelected = isSeen
      ? resetSeenCheckKeys.includes(check.key)
      : resetCheckKeys.includes(check.key);
    const toggle = () => {
      if (isSeen) {
        isSelected ? removeResetSeenCheck(check.key) : addResetSeenCheck(check.key);
      } else {
        isSelected ? removeResetCheck(check.key) : addResetCheck(check.key);
      }
    };

    return (
      <div
        key={check.key}
        className={`flex items-start gap-3 p-3 rounded border transition-colors ${isSelected ? "bg-primary/10 border-primary/30" : "hover:bg-accent/50"}`}
      >
        <Checkbox checked={isSelected} onCheckedChange={toggle} className="mt-0.5" />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-mono text-xs truncate">{check.flag_name}</span>
            <Badge variant="outline" className="text-[10px]">{check.skill_display_name}</Badge>
            <Badge variant="secondary" className="text-[10px]">DC {check.difficulty}</Badge>
          </div>
          <div className="flex gap-4 mt-1 text-[10px] text-muted-foreground">
            <span>Skill: {check.last_skill_value}</span>
            <span>Target: {check.last_target_value}</span>
            {check.check_precondition && <span>Cond: {check.check_precondition}</span>}
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold flex items-center gap-2">
          <RotateCcw className="h-5 w-5" />
          White Checks
        </h2>
        <div className="flex items-center gap-2">
          {resetCheckKeys.length + resetSeenCheckKeys.length > 0 && (
            <Badge variant="destructive">
              {resetCheckKeys.length + resetSeenCheckKeys.length} selected for reset
            </Badge>
          )}
        </div>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search checks..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      <Tabs defaultValue="failed">
        <TabsList>
          <TabsTrigger value="failed">
            Failed Checks ({currentSave.failed_checks.length})
          </TabsTrigger>
          <TabsTrigger value="seen">
            Seen Checks ({currentSave.seen_checks.length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="failed">
          <div className="flex gap-2 mb-4">
            <Button variant="outline" size="sm" onClick={selectAllFailed}>Select All</Button>
            <Button variant="outline" size="sm" onClick={clearAllFailed}>Clear Selection</Button>
          </div>
          <ScrollArea className="h-[calc(100vh-20rem)]">
            <div className="space-y-2">
              {filterChecks(currentSave.failed_checks).map((c) => renderCheck(c, false))}
            </div>
          </ScrollArea>
        </TabsContent>

        <TabsContent value="seen">
          <div className="flex gap-2 mb-4">
            <Button variant="outline" size="sm" onClick={selectAllSeen}>Select All</Button>
            <Button variant="outline" size="sm" onClick={clearAllSeen}>Clear Selection</Button>
          </div>
          <ScrollArea className="h-[calc(100vh-20rem)]">
            <div className="space-y-2">
              {filterChecks(currentSave.seen_checks).map((c) => renderCheck(c, true))}
            </div>
          </ScrollArea>
        </TabsContent>
      </Tabs>
    </div>
  );
}
