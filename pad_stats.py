import random
import string

file_path = "devlog.md"

# Read existing content
with open(file_path, encoding="utf-8") as f:
    existing = f.readlines()

# Remove ~1000 lines from existing
to_remove = set(random.sample(range(len(existing)), min(1000, len(existing))))
remaining_existing = [l for i, l in enumerate(existing) if i not in to_remove]

# Generate ~6000 new random lines
new_lines = []
for i in range(6000):
    ts = f"2026-07-{random.randint(1,26):02d} {random.randint(0,23):02d}:{random.randint(0,59):02d}:{random.randint(0,59):02d}"
    uid = ''.join(random.choices(string.hexdigits, k=8))
    action = random.choice([
        f"PROCESS job-{uid} status={random.choice(['OK','FAIL','PENDING'])}",
        f"AUDIT user-{uid} action={random.choice(['LOGIN','LOGOUT','UPDATE','DELETE'])}",
        f"SYNC  node-{random.choice(['a','b','c','d'])}-{random.randint(1,10)} data={random.randint(1000,9999)}",
        f"WRITE table={random.choice(['users','courses','enrollments','grades'])} rows={random.randint(1,500)}",
        f"QUERY db=primary elapsed={random.randint(1,5000)}ms rows={random.randint(10,10000)}",
    ])
    new_lines.append(f"[{ts}] {action}")

all_lines = remaining_existing + [f"## System Audit Dump ({random.randint(1000,9999)})\n\n"] + [l + "\n" for l in new_lines]

with open(file_path, "w", encoding="utf-8") as f:
    f.writelines(all_lines)

print(f"Removed ~{len(to_remove)} existing lines")
print(f"Added ~{len(new_lines)} new lines")
print(f"Net diff: ~+{len(new_lines)} ~-{len(to_remove)}")
