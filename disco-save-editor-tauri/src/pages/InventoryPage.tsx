import { useEffect, useState } from "react";
import { useStore } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Search, Plus, Package, Trash2, Info, ShoppingBag, ShieldCheck, Zap, Ghost } from "lucide-react";
import type { InventoryItemDisplay } from "@/types";

export default function InventoryPage() {
  const { currentSave, updateField, catalogItems, catalogLoading, loadCatalogItems } = useStore();
  const [search, setSearch] = useState("");
  const [catalogSearch, setCatalogSearch] = useState("");

  useEffect(() => {
    if (catalogItems.length === 0) loadCatalogItems();
  }, [catalogItems.length, loadCatalogItems]);

  if (!currentSave) return null;

  const filteredItems = currentSave.owned_items.filter(
    (i) =>
      i.display_name.toLowerCase().includes(search.toLowerCase()) ||
      i.name.toLowerCase().includes(search.toLowerCase())
  );

  const filteredCatalog = catalogItems.filter(
    (i) =>
      !currentSave.owned_items.some((o) => o.name === i.name) &&
      (i.display_name.toLowerCase().includes(catalogSearch.toLowerCase()) ||
        i.name.toLowerCase().includes(catalogSearch.toLowerCase()))
  );

  const updateItem = (idx: number, field: keyof InventoryItemDisplay, value: unknown) => {
    const updated = [...currentSave.owned_items];
    updated[idx] = { ...updated[idx], [field]: value };
    updateField("owned_items", updated);
  };

  const removeItem = (idx: number) => {
    const updated = currentSave.owned_items.filter((_, i) => i !== idx);
    updateField("owned_items", updated);
  };

  const addItem = (item: any) => {
    const newItem: InventoryItemDisplay = {
      name: item.name,
      display_name: item.display_name,
      description: item.description,
      bonus: item.bonus,
      is_owned: true,
      is_equipped: false,
      equip_slot: "",
      is_quest_item: item.is_quest_item,
      is_cursed: item.is_cursed,
      is_substance: item.is_substance,
      substance_uses: item.is_substance ? 1 : 0,
    };
    updateField("owned_items", [...currentSave.owned_items, newItem]);
  };

  return (
    <TooltipProvider>
      <div className="space-y-6 max-w-6xl mx-auto h-full">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <h2 className="text-2xl font-bold flex items-center gap-2">
              <ShoppingBag className="h-6 w-6 text-primary" />
              Inventory & Equipment
            </h2>
            <p className="text-sm text-muted-foreground">Manage character clothing, tools, and consumables.</p>
          </div>
          <div className="flex items-center gap-4 bg-muted/30 px-3 py-1.5 rounded-lg border">
            <div className="flex items-center gap-2">
              <Label className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Bullets</Label>
              <Input
                type="number"
                value={currentSave.bullets}
                onChange={(e) => updateField("bullets", parseInt(e.target.value) || 0)}
                className="w-20 h-8 font-mono bg-background"
              />
            </div>
          </div>
        </div>

        <Tabs defaultValue="owned" className="w-full">
          <TabsList className="grid grid-cols-2 max-w-[400px] mb-6">
            <TabsTrigger value="owned" className="flex items-center gap-2">
              Backpack
              <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">{currentSave.owned_items.length}</Badge>
            </TabsTrigger>
            <TabsTrigger value="catalog" className="flex items-center gap-2">
              Item Store
              <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">{catalogItems.length}</Badge>
            </TabsTrigger>
          </TabsList>

          <TabsContent value="owned" className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search owned items..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9 h-11 bg-muted/20"
              />
            </div>
            
            <ScrollArea className="h-[calc(100vh-320px)] pr-4">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 pb-8">
                {filteredItems.map((item, idx) => (
                  <Card key={`${item.name}-${idx}`} className="hover:border-primary/30 transition-all flex flex-col">
                    <CardHeader className="py-3 px-4 flex flex-row items-center justify-between space-y-0 bg-muted/20">
                      <div className="flex items-center gap-2 min-w-0">
                        <Package className="h-4 w-4 text-primary flex-shrink-0" />
                        <CardTitle className="text-xs font-bold truncate uppercase tracking-tight">
                          {item.display_name}
                        </CardTitle>
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-6 w-6 text-muted-foreground hover:text-destructive"
                        onClick={() => removeItem(idx)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </CardHeader>
                    <CardContent className="px-4 py-3 space-y-4 flex-1 flex flex-col">
                      <div className="space-y-1">
                         <div className="flex flex-wrap gap-1.5 mb-2">
                          {item.is_quest_item && <Badge variant="secondary" className="bg-amber-500/10 text-amber-500 border-amber-500/20 text-[8px] h-4">QUEST</Badge>}
                          {item.is_cursed && <Badge variant="secondary" className="bg-red-500/10 text-red-500 border-red-500/20 text-[8px] h-4">CURSED</Badge>}
                          {item.is_substance && <Badge variant="secondary" className="bg-green-500/10 text-green-500 border-green-500/20 text-[8px] h-4">STIM</Badge>}
                        </div>
                        <p className="text-[11px] leading-snug line-clamp-3 text-muted-foreground italic">
                          {item.description}
                        </p>
                      </div>

                      <div className="mt-auto space-y-3 pt-3 border-t border-dashed">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                             <Label className="text-[10px] font-bold">Equipped</Label>
                             <Switch
                              checked={item.is_equipped}
                              onCheckedChange={(v) => updateItem(idx, "is_equipped", v)}
                              className="scale-75"
                            />
                          </div>
                          {item.bonus && (
                            <Tooltip>
                              <TooltipTrigger>
                                <Info className="h-3.5 w-3.5 text-primary opacity-60" />
                              </TooltipTrigger>
                              <TooltipContent className="max-w-[200px] text-[10px]">
                                {item.bonus}
                              </TooltipContent>
                            </Tooltip>
                          )}
                        </div>
                        {item.is_substance && (
                          <div className="flex items-center gap-2">
                            <Label className="text-[10px] uppercase font-bold text-muted-foreground">Uses Remaining</Label>
                            <Input
                              type="number"
                              value={item.substance_uses}
                              onChange={(e) => updateItem(idx, "substance_uses", parseInt(e.target.value) || 0)}
                              className="h-7 w-16 text-xs font-mono"
                            />
                          </div>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="catalog" className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search catalog for new items..."
                value={catalogSearch}
                onChange={(e) => setCatalogSearch(e.target.value)}
                className="pl-9 h-11 bg-muted/20"
              />
            </div>

            <ScrollArea className="h-[calc(100vh-320px)] pr-4">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 pb-8">
                {filteredCatalog.length > 0 ? (
                  filteredCatalog.slice(0, 100).map((item, idx) => (
                    <Card key={`${item.name}-${idx}`} className="text-sm bg-muted/5 border-dashed">
                      <CardHeader className="py-3 px-4 flex flex-row items-center justify-between space-y-0">
                        <CardTitle className="text-xs font-bold truncate uppercase tracking-tight">
                          {item.display_name}
                        </CardTitle>
                        <Button
                          variant="secondary"
                          size="sm"
                          className="h-7 text-[10px] px-2 font-bold"
                          onClick={() => addItem(item)}
                        >
                          <Plus className="h-3 w-3 mr-1" />
                          ACQUIRE
                        </Button>
                      </CardHeader>
                      <CardContent className="px-4 pb-3">
                         <p className="text-[10px] text-muted-foreground line-clamp-1 italic mb-1">
                          {item.description || "Historical data available elsewhere."}
                        </p>
                        <div className="flex flex-wrap gap-1">
                          {item.is_quest_item && <Badge className="text-[7px] h-3 px-1">Q</Badge>}
                          {item.is_cursed && <Badge variant="destructive" className="text-[7px] h-3 px-1">C</Badge>}
                          {item.is_substance && <Badge className="bg-green-500 text-white text-[7px] h-3 px-1">S</Badge>}
                        </div>
                      </CardContent>
                    </Card>
                  ))
                ) : (
                  <div className="col-span-full py-20 text-center border-2 border-dashed rounded-xl">
                    <p className="text-muted-foreground">No matching items in catalog.</p>
                  </div>
                )}
              </div>
            </ScrollArea>
          </TabsContent>
        </Tabs>
      </div>
    </TooltipProvider>
  );
}
