"""
Extract game data from Disco Elysium Unity asset bundles.
Uses UnityPy to read the PixelCrushers DialogueDatabase from the dialoguebundle.
"""
import UnityPy
import json
import os
import sys

GAME_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AA_DIR = os.path.join(GAME_DIR, "disco_Data", "StreamingAssets", "aa", "StandaloneWindows64")
OUTPUT_DIR = os.path.join(GAME_DIR, "output", "game_assets")

def explore_bundle(bundle_path, verbose=False):
    """Load a bundle and list all objects inside it."""
    env = UnityPy.load(bundle_path)
    print(f"\nBundle: {os.path.basename(bundle_path)}")
    print(f"  Objects: {len(env.objects)}")
    
    type_counts = {}
    for obj in env.objects:
        t = obj.type.name
        type_counts[t] = type_counts.get(t, 0) + 1
        if verbose and t in ("MonoBehaviour", "TextAsset", "MonoScript"):
            try:
                data = obj.read()
                name = getattr(data, 'name', getattr(data, 'm_Name', '???'))
                print(f"    {t}: {name}")
            except Exception as e:
                print(f"    {t}: ERROR reading - {e}")
    
    for t, c in sorted(type_counts.items()):
        print(f"  {t}: {c}")
    return env

def try_read_typetree(obj):
    """Try to read an object using type tree."""
    try:
        return obj.read_typetree()
    except Exception:
        return None

def extract_dialogue_bundle():
    """Extract dialogue database from the main dialogue bundle."""
    # Find the dialogue bundle
    dialogue_bundle = None
    for f in os.listdir(AA_DIR):
        if f.startswith("dialoguebundle"):
            dialogue_bundle = os.path.join(AA_DIR, f)
            break
    
    if not dialogue_bundle:
        print("ERROR: dialoguebundle not found!")
        return
    
    print(f"Loading dialogue bundle: {os.path.basename(dialogue_bundle)}")
    env = UnityPy.load(dialogue_bundle)
    print(f"Found {len(env.objects)} objects")
    
    # Categorize objects
    results = {
        "monobehaviours": [],
        "text_assets": [],
        "scripts": [],
        "other": []
    }
    
    for obj in env.objects:
        t = obj.type.name
        
        if t == "MonoBehaviour":
            # Try type tree first (works if embedded)
            tree = try_read_typetree(obj)
            if tree:
                name = tree.get("m_Name", "unnamed")
                print(f"  MonoBehaviour (typetree): {name}")
                # Check for dialogue database fields
                if any(k in tree for k in ["conversations", "actors", "items", "variables"]):
                    print("    >>> DIALOGUE DATABASE FOUND! <<<")
                    results["monobehaviours"].append({"name": name, "data": tree, "is_dialogue_db": True})
                else:
                    results["monobehaviours"].append({"name": name, "keys": list(tree.keys())[:20]})
            else:
                # Try regular read
                try:
                    data = obj.read()
                    name = getattr(data, 'm_Name', 'unnamed')
                    print(f"  MonoBehaviour (read): {name}")
                    # Dump raw bytes for inspection
                    if hasattr(data, 'raw_data'):
                        raw = data.raw_data
                        if len(raw) > 1000:
                            print(f"    Raw data: {len(raw)} bytes")
                            results["monobehaviours"].append({"name": name, "raw_size": len(raw)})
                        else:
                            results["monobehaviours"].append({"name": name, "raw_size": len(raw)})
                except Exception as e:
                    print(f"  MonoBehaviour: ERROR - {e}")
                    results["monobehaviours"].append({"error": str(e)})
        
        elif t == "TextAsset":
            try:
                data = obj.read()
                name = data.m_Name
                text = data.m_Script
                if isinstance(text, bytes):
                    text_str = text.decode('utf-8', errors='replace')[:500]
                else:
                    text_str = str(text)[:500]
                print(f"  TextAsset: {name} ({len(text)} bytes)")
                results["text_assets"].append({"name": name, "size": len(text), "preview": text_str})
            except Exception as e:
                print(f"  TextAsset: ERROR - {e}")
        
        elif t == "MonoScript":
            try:
                data = obj.read()
                name = getattr(data, 'm_Name', getattr(data, 'm_ClassName', '???'))
                ns = getattr(data, 'm_Namespace', '')
                print(f"  MonoScript: {ns}.{name}")
                results["scripts"].append({"name": name, "namespace": ns})
            except Exception as e:
                print(f"  MonoScript: ERROR - {e}")
        
        else:
            results["other"].append({"type": t})
    
    return results

def extract_all_bundles_summary():
    """Quick survey of what's in each type of bundle."""
    bundle_types = {}
    for f in os.listdir(AA_DIR):
        if not f.endswith(".bundle"):
            continue
        # Extract the bundle category from the name
        parts = f.split("_assets_all_")
        if parts:
            category = parts[0]
            if category not in bundle_types:
                bundle_types[category] = {
                    "count": 0,
                    "total_size": 0,
                    "example": f
                }
            bundle_types[category]["count"] += 1
            bundle_types[category]["total_size"] += os.path.getsize(os.path.join(AA_DIR, f))
    
    print("\n=== Bundle Categories ===")
    for cat, info in sorted(bundle_types.items()):
        mb = info["total_size"] / (1024*1024)
        print(f"  {cat}: {info['count']} bundles, {mb:.2f} MB")

def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    
    if len(sys.argv) > 1 and sys.argv[1] == "survey":
        extract_all_bundles_summary()
        return
    
    if len(sys.argv) > 1 and sys.argv[1] == "explore":
        bundle_name = sys.argv[2] if len(sys.argv) > 2 else "dialoguebundle"
        for f in os.listdir(AA_DIR):
            if f.startswith(bundle_name) and f.endswith(".bundle"):
                explore_bundle(os.path.join(AA_DIR, f), verbose=True)
                break
        return
    
    # Default: extract dialogue database
    results = extract_dialogue_bundle()
    
    if results:
        out_path = os.path.join(OUTPUT_DIR, "_extraction_report.json")
        # Serialize safely
        def safe_serialize(obj):
            if isinstance(obj, bytes):
                return f"<{len(obj)} bytes>"
            if isinstance(obj, (dict, list, str, int, float, bool, type(None))):
                return obj
            return str(obj)
        
        with open(out_path, 'w', encoding='utf-8') as f:
            json.dump(results, f, indent=2, default=safe_serialize)
        print(f"\nResults saved to {out_path}")

if __name__ == "__main__":
    main()
