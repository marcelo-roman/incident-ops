import re
import sys
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SKIP_DIRS = {
    ".git",
    ".venv",
    ".pytest_cache",
    ".mypy_cache",
    ".ruff_cache",
    "node_modules",
    "bin",
    "obj",
    "site",
    "dist",
    "coverage",
    "TestResults",
    "playwright-report",
}
LINK = re.compile(r"(?<!!)\[[^\]]*\]\(([^)\s]+)\)")
HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")
FENCE = re.compile(r"^\s*(```|~~~)")


def github_slug(text: str) -> str:
    text = re.sub(r"`|\*|_(?=\w)|(?<=\w)_", "", text)
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)
    text = unicodedata.normalize("NFKC", text).lower()
    text = "".join(ch for ch in text if ch.isalnum() or ch in " -_")
    return text.replace(" ", "-")


def strip_fences(lines: list[str]) -> list[str]:
    kept, inside = [], False
    for line in lines:
        if FENCE.match(line):
            inside = not inside
            continue
        if not inside:
            kept.append(line)
    return kept


def anchors(path: Path) -> set[str]:
    seen: dict[str, int] = {}
    result = set()
    for line in strip_fences(path.read_text(encoding="utf-8").splitlines()):
        match = HEADING.match(line)
        if not match:
            continue
        slug = github_slug(match.group(2))
        count = seen.get(slug, 0)
        seen[slug] = count + 1
        result.add(slug if count == 0 else f"{slug}-{count}")
    return result


def markdown_files() -> list[Path]:
    return [p for p in ROOT.rglob("*.md") if not SKIP_DIRS.intersection(p.relative_to(ROOT).parts)]


def check(path: Path) -> list[str]:
    errors = []
    body = "\n".join(strip_fences(path.read_text(encoding="utf-8").splitlines()))
    for target in LINK.findall(body):
        if re.match(r"^[a-z]+:", target):
            continue
        file_part, _, fragment = target.partition("#")
        resolved = (path.parent / file_part).resolve() if file_part else path
        if not resolved.exists():
            errors.append(f"{path.relative_to(ROOT)}: missing target {target}")
            continue
        if fragment and resolved.suffix == ".md" and fragment not in anchors(resolved):
            errors.append(f"{path.relative_to(ROOT)}: missing anchor {target}")
    return errors


def main() -> int:
    errors = [error for path in markdown_files() for error in check(path)]
    for error in errors:
        print(error)
    print(f"checked {len(markdown_files())} files, {len(errors)} broken links")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
