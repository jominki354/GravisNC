import os

log_path = r"d:/GravisNC/src/03.App/GCode.App.WPF/build_errors_v3.log"


def search_log(path):
    print(f"Reading {path}...")
    try:
        # Try UTF-8 first
        with open(path, "r", encoding="utf-8") as f:
            content = f.read()
    except UnicodeDecodeError:
        try:
            # Try UTF-16 (common for Windows logs)
            with open(path, "r", encoding="utf-16") as f:
                content = f.read()
        except UnicodeDecodeError:
            # Fallback to latin-1
            with open(path, "r", encoding="latin-1") as f:
                content = f.read()

    print(f"File read successfully. Length: {len(content)} chars")

    keywords = [
        "StructurePanel",
        "MarkupCompilePass1",
        "CoreCompile",
        "error",
        "오류",
        "warning",
        "경고",
    ]

    for kw in keywords:
        print(f"--- Searching for '{kw}' ---")
        count = content.count(kw)
        print(f"Found {count} occurrences.")
        if count > 0:
            # Print first 200 chars of context for the first few occurrences
            start_idx = 0
            for i in range(min(5, count)):
                idx = content.find(kw, start_idx)
                start = max(0, idx - 100)
                end = min(len(content), idx + 100)
                print(
                    f"Match {i + 1}: ...{content[start:end].replace(chr(10), ' ').replace(chr(13), ' ')}..."
                )
                start_idx = idx + 1

    # Check specifically for StructurePanel context
    idx = content.find("StructurePanel")
    if idx != -1:
        print("\nStructurePanel Context:")
        print(content[max(0, idx - 500) : min(len(content), idx + 500)])


if __name__ == "__main__":
    search_log(log_path)
