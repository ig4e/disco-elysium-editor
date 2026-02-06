import { useState } from "react";
import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Search, Package } from "lucide-react";

export default function ContainersPage() {
  const { currentSave } = useStore();
  const [search, setSearch] = useState("");

  if (!currentSave) return null;

  const filtered = currentSave.containers.filter(
    (c) =>
      c.container_id.toLowerCase().includes(search.toLowerCase()) ||
      c.items.some((i) => i.name.toLowerCase().includes(search.toLowerCase()))
  );

  const nonEmpty = filtered.filter((c) => c.item_count > 0);
  const empty = filtered.filter((c) => c.item_count === 0);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold flex items-center gap-2">
          <Package className="h-5 w-5" />
          Containers
        </h2>
        <div className="flex gap-2">
          <Badge variant="outline">{currentSave.containers.length} total</Badge>
          <Badge variant="secondary">{currentSave.containers.filter((c) => c.item_count > 0).length} with items</Badge>
        </div>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search containers or items..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      <ScrollArea className="h-[calc(100vh-14rem)]">
        <Accordion type="multiple" className="space-y-1">
          {nonEmpty.map((container) => (
            <AccordionItem key={container.container_id} value={container.container_id}>
              <AccordionTrigger className="text-sm py-2 px-3 hover:no-underline">
                <div className="flex items-center gap-3 flex-1 min-w-0">
                  <span className="font-mono text-xs truncate">{container.container_id}</span>
                  <Badge variant="secondary" className="text-[10px]">
                    {container.item_count} items
                  </Badge>
                  {container.total_value > 0 && (
                    <Badge variant="outline" className="text-[10px]">
                      Value: {container.total_value}
                    </Badge>
                  )}
                </div>
              </AccordionTrigger>
              <AccordionContent>
                <div className="px-3 pb-2 space-y-1">
                  {container.items.map((item, i) => (
                    <div
                      key={`${item.name}-${i}`}
                      className="flex items-center gap-3 text-xs p-2 rounded bg-accent/30"
                    >
                      <span className="flex-1 truncate">{item.name}</span>
                      <span className="text-muted-foreground">Prob: {item.probability}%</span>
                      <span className="text-muted-foreground">Val: {item.calculated_value}</span>
                      {item.bonus_loot && (
                        <Badge className="text-[9px] px-1 py-0 bg-yellow-500/20 text-yellow-400">Bonus</Badge>
                      )}
                    </div>
                  ))}
                </div>
              </AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>

        {empty.length > 0 && (
          <div className="mt-4">
            <p className="text-xs text-muted-foreground mb-2">Empty containers ({empty.length})</p>
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-1">
              {empty.map((c) => (
                <div key={c.container_id} className="text-[10px] font-mono text-muted-foreground p-1 truncate">
                  {c.container_id}
                </div>
              ))}
            </div>
          </div>
        )}
      </ScrollArea>
    </div>
  );
}
