"""Fail CI when a NuGet package has a known High or Critical vulnerability (issue #6).

Reads the JSON report from:
    dotnet list <solution> package --vulnerable --include-transitive --format json
`dotnet list` exits 0 even when it finds vulnerabilities, so this script does the gating.
Writes a Markdown table to $GITHUB_STEP_SUMMARY when it is set.

Usage: python3 check_vulnerable_packages.py vulnerable.json
"""

import json
import os
import sys
from pathlib import PurePath

BLOCKING = {"high", "critical"}


def findings(report):
    for project in report.get("projects", []):
        name = PurePath(project.get("path", "?")).name
        for framework in project.get("frameworks", []):
            for kind in ("topLevelPackages", "transitivePackages"):
                for package in framework.get(kind, []):
                    for vuln in package.get("vulnerabilities", []):
                        yield {
                            "project": name,
                            "package": package.get("id", "?"),
                            "version": package.get("resolvedVersion", "?"),
                            "severity": vuln.get("severity", "Unknown"),
                            "advisory": vuln.get("advisoryurl", ""),
                        }


def main(path):
    with open(path, encoding="utf-8-sig") as f:
        report = json.load(f)
    rows = list(findings(report))

    lines = ["### Vulnerable NuGet packages", ""]
    if rows:
        lines += ["| Project | Package | Version | Severity | Advisory |", "|---|---|---|---|---|"]
        lines += [f"| {r['project']} | {r['package']} | {r['version']} | {r['severity']} | {r['advisory']} |" for r in rows]
    else:
        lines.append("No known vulnerable packages.")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as f:
            f.write("\n".join(lines) + "\n")
    print("\n".join(lines))

    blocking = [r for r in rows if r["severity"].lower() in BLOCKING]
    if blocking:
        print(f"::error title=Vulnerable packages::{len(blocking)} package(s) with High or Critical vulnerabilities. See the job summary.")
        return 1
    if rows:
        print("::warning title=Vulnerable packages::Low or Moderate vulnerabilities found. See the job summary.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
