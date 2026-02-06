import { useState } from "react";
import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Search, CheckCircle, Clock, BookOpen, AlertCircle, Calendar } from "lucide-react";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";

export default function JournalPage() {
  const { currentSave, updateField } = useStore();
  const [search, setSearch] = useState("");

  if (!currentSave) return null;

  const activeTasks = currentSave.tasks.filter((t) => !t.is_resolved);
  const completedTasks = currentSave.tasks.filter((t) => t.is_resolved);

  const filterTasks = (tasks: typeof currentSave.tasks) =>
    tasks.filter(
      (t) =>
        t.task_name.toLowerCase().includes(search.toLowerCase()) ||
        t.description.toLowerCase().includes(search.toLowerCase())
    );

  const toggleResolved = (taskName: string) => {
    const updated = currentSave.tasks.map((t) =>
      t.task_name === taskName ? { ...t, is_resolved: !t.is_resolved } : t
    );
    updateField("tasks", updated);
  };

  const toggleNew = (taskName: string) => {
    const updated = currentSave.tasks.map((t) =>
      t.task_name === taskName ? { ...t, is_new: !t.is_new } : t
    );
    updateField("tasks", updated);
  };

  const renderTask = (task: typeof currentSave.tasks[0]) => (
    <Card key={task.task_name} className={`text-sm transition-all hover:border-primary/30 ${task.is_resolved ? 'bg-muted/20' : 'border-primary/10'}`}>
      <CardHeader className="py-4 px-5 space-y-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            {task.is_resolved ? (
              <CheckCircle className="h-5 w-5 text-green-500" />
            ) : (
              <AlertCircle className="h-5 w-5 text-amber-500" />
            )}
            <CardTitle className="text-base font-bold tracking-tight">
              {task.task_name.replace("TASK.", "").replace(/_/g, " ").toUpperCase()}
            </CardTitle>
          </div>
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2 bg-background/50 px-2 py-1 rounded border text-xs">
               <Label className="text-[10px] font-bold">New</Label>
               <Switch
                checked={task.is_new}
                onCheckedChange={() => toggleNew(task.task_name)}
                className="scale-75"
              />
            </div>
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                   <div className="flex items-center gap-2 bg-background/50 px-2 py-1 rounded border text-xs">
                    <Label className="text-[10px] font-bold">Resolved</Label>
                    <Switch
                      checked={task.is_resolved}
                      onCheckedChange={() => toggleResolved(task.task_name)}
                      className="scale-75"
                    />
                  </div>
                </TooltipTrigger>
                <TooltipContent>Marking as resolved moves it to the Completed tab</TooltipContent>
              </Tooltip>
            </TooltipProvider>
          </div>
        </div>
        <CardDescription className="text-xs leading-relaxed italic text-muted-foreground/80">
          {task.description || "No description available in save data."}
        </CardDescription>
      </CardHeader>
      <CardContent className="px-5 pb-4 space-y-3">
        {task.subtasks && task.subtasks.length > 0 && (
          <div className="space-y-2">
            <Label className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Objectives</Label>
            <div className="grid grid-cols-1 gap-1.5">
              {task.subtasks.map((sub, idx) => (
                <div key={idx} className="flex items-center gap-2 text-[11px] bg-muted/40 p-2 rounded-md">
                  <div className="h-1 w-1 bg-primary rounded-full" />
                  {sub.replace(/_/g, " ")}
                </div>
              ))}
            </div>
          </div>
        )}
        <div className="flex items-center justify-between pt-2 border-t border-dashed">
          <div className="flex items-center gap-1.5 text-[10px] font-mono text-muted-foreground">
            <Calendar className="h-3 w-3" />
            {task.acquired_time || "Day 1, 08:00"}
          </div>
          <Badge variant="outline" className="text-[9px] font-mono opacity-50">
            {task.task_name}
          </Badge>
        </div>
      </CardContent>
    </Card>
  );

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h2 className="text-2xl font-bold flex items-center gap-2">
            <BookOpen className="h-6 w-6 text-primary" />
            Case Journal
          </h2>
          <p className="text-sm text-muted-foreground">Track and manage active tasks and completed case objectives.</p>
        </div>
        <Badge variant="outline" className="px-4 py-1">
          {currentSave.tasks.length} Total Logs
        </Badge>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Filter by name or description..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9 h-11 bg-muted/20"
        />
      </div>

      <Tabs defaultValue="active" className="w-full">
        <TabsList className="grid w-full grid-cols-2 max-w-[400px] mb-6">
          <TabsTrigger value="active" className="flex items-center gap-2">
            Active
            <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">{activeTasks.length}</Badge>
          </TabsTrigger>
          <TabsTrigger value="completed" className="flex items-center gap-2">
            Completed
            <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">{completedTasks.length}</Badge>
          </TabsTrigger>
        </TabsList>

        <TabsContent value="active">
          <ScrollArea className="h-[calc(100vh-320px)] pr-4">
            <div className="grid grid-cols-1 gap-4 pb-8">
              {filterTasks(activeTasks).length > 0 ? (
                filterTasks(activeTasks).map(renderTask)
              ) : (
                <div className="text-center py-20 bg-muted/10 border-2 border-dashed rounded-xl">
                  <Clock className="h-10 w-10 mx-auto text-muted-foreground/30 mb-3" />
                  <p className="text-muted-foreground">No active tasks found matching your search.</p>
                </div>
              )}
            </div>
          </ScrollArea>
        </TabsContent>

        <TabsContent value="completed">
          <ScrollArea className="h-[calc(100vh-320px)] pr-4">
            <div className="grid grid-cols-1 gap-4 pb-8">
              {filterTasks(completedTasks).length > 0 ? (
                filterTasks(completedTasks).map(renderTask)
              ) : (
                <div className="text-center py-20 bg-muted/10 border-2 border-dashed rounded-xl">
                  <CheckCircle className="h-10 w-10 mx-auto text-muted-foreground/30 mb-3" />
                  <p className="text-muted-foreground">No completed tasks found.</p>
                </div>
              )}
            </div>
          </ScrollArea>
        </TabsContent>
      </Tabs>
    </div>
  );
}
