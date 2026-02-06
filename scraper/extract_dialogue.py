"""
Extract the PixelCrushers DialogueDatabase from the Disco Elysium dialogue bundle.
"""
import UnityPy
import json
import os
import sys

GAME_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AA_DIR = os.path.join(GAME_DIR, "disco_Data", "StreamingAssets", "aa", "StandaloneWindows64")
OUTPUT_DIR = os.path.join(GAME_DIR, "output", "game_assets")

def find_bundle(prefix):
    for f in os.listdir(AA_DIR):
        if f.startswith(prefix) and f.endswith(".bundle"):
            return os.path.join(AA_DIR, f)
    return None

def extract_dialogue_db():
    bundle_path = find_bundle("dialoguebundle")
    if not bundle_path:
        print("dialoguebundle not found!")
        return

    env = UnityPy.load(bundle_path)
    
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        
        print(f"Object type: {obj.type.name}, pathId: {obj.path_id}")
        
        # Try reading with type tree
        print("Attempting type tree read...")
        try:
            tree = obj.read_typetree()
            print(f"Type tree keys: {list(tree.keys())[:30]}")
            
            # Save full type tree
            os.makedirs(OUTPUT_DIR, exist_ok=True)
            out = os.path.join(OUTPUT_DIR, "dialogue_db_typetree.json")
            
            def safe_json(o):
                if isinstance(o, bytes):
                    return f"<{len(o)} bytes>"
                return str(o)
            
            with open(out, 'w', encoding='utf-8') as f:
                json.dump(tree, f, indent=2, default=safe_json, ensure_ascii=False)
            print(f"Saved type tree to {out}")
            size_mb = os.path.getsize(out) / (1024*1024)
            print(f"Output size: {size_mb:.2f} MB")
            
            # Report top-level structure
            for key in tree:
                val = tree[key]
                if isinstance(val, list):
                    print(f"  {key}: list[{len(val)}]")
                    if len(val) > 0 and isinstance(val[0], dict):
                        print(f"    first item keys: {list(val[0].keys())[:15]}")
                elif isinstance(val, dict):
                    print(f"  {key}: dict with {len(val)} keys")
                elif isinstance(val, str):
                    print(f"  {key}: str({len(val)} chars)")
                else:
                    print(f"  {key}: {type(val).__name__} = {str(val)[:100]}")
            
            return tree
            
        except Exception as e:
            print(f"Type tree failed: {e}")
            print("Trying raw read...")
            
            try:
                data = obj.read()
                print(f"Name: {data.m_Name}")
                
                # Get the raw MonoBehaviour data
                if hasattr(data, 'raw_data'):
                    raw = data.raw_data
                    print(f"Raw data size: {len(raw)} bytes ({len(raw)/1024/1024:.2f} MB)")
                    
                    # Save raw bytes for analysis
                    raw_path = os.path.join(OUTPUT_DIR, "dialogue_db_raw.bin")
                    os.makedirs(OUTPUT_DIR, exist_ok=True)
                    with open(raw_path, 'wb') as f:
                        f.write(raw)
                    print(f"Saved raw data to {raw_path}")
                    
                    # Try to find readable strings
                    text = raw.decode('utf-8', errors='replace')
                    # Find field-like patterns
                    import re
                    patterns = re.findall(r'[\x20-\x7E]{10,}', text[:100000])
                    print(f"\nFirst 50 readable strings in raw data:")
                    for s in patterns[:50]:
                        print(f"  {s[:120]}")
                    
            except Exception as e2:
                print(f"Raw read also failed: {e2}")

if __name__ == "__main__":
    extract_dialogue_db()
