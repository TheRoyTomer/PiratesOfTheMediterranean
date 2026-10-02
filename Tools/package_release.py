"""Package successful local builds; preserve macOS executable permissions in ZIP."""
from pathlib import Path
import hashlib, json, re, shutil, stat, struct, zipfile

root = Path(__file__).resolve().parents[1]
builds = root / 'Builds'
source = (root / 'Assets/Scenes/MainMenu.unity').read_text(encoding='utf-8-sig')
match = re.search(r'  m_text: ("<size=44><b>Chief.*?)(?=\n  m_isRightToLeft:)', source, re.S)
assert match, 'Missing first credit'
credits = re.sub(r'<[^>]+>', '', json.loads(re.sub(r'\r?\n\s+', ' ', match[1])))
assert credits.startswith('Chief Morale Officer & Captain’s Better Half\n\nYuval Bashan')
audio = '''Pirates of the Mediterranean - Audio credits

Item Pickup sound - Strechy
https://freesound.org/people/Strechy/sounds/654251/
CC BY 4.0: https://creativecommons.org/licenses/by/4.0/
Used as Pickup_Collect.ogg. Renamed; playback volume adjusted.

Change Weapon Sound - knova
https://freesound.org/people/knova/sounds/170273/
CC BY 4.0: https://creativecommons.org/licenses/by/4.0/
Used as UI_SwitchFireDirection.wav. Renamed; playback volume adjusted.

The Buccaneer's Haul - Shane Ivers
Copyright 2016 Shane Ivers
https://www.silvermansound.com/free-music/the-buccaneers-haul
CC BY 4.0: https://creativecommons.org/licenses/by/4.0/
Used as Music_MainMenu.mp3. Renamed; Unity import transcoding and playback volume adjustments.

Additional sound effects are CC0. See Credits.txt for their creators.
'''
for folder in ['Windows', 'MacUniversal']:
    target = builds / folder
    report = (target / 'build-report.txt').read_text(encoding='utf-8-sig')
    assert 'Result: Succeeded\nErrors: 0\n' in report, report[:300]
    (target / 'Credits.txt').write_text(credits + '\n', encoding='utf-8')
    (target / 'Audio-Credits.txt').write_text(audio, encoding='utf-8')

mac = builds / 'MacUniversal'
app = mac / 'PiratesOfTheMediterranean.app'
(mac / 'README.txt').write_text('''Pirates of the Mediterranean - macOS Universal
Intel (x86_64) and Apple Silicon (arm64). Requires macOS 12.0 or later.
Unity 6000.3.20f1, Mono, non-development build.

Transfer the ZIP to your Mac and extract it using Finder, then open the .app.
The ZIP preserves executable permissions for transfer from Windows.
This local test build has no Developer ID signing or Apple notarization.
If macOS cannot verify the developer, after attempting to open your trusted copy,
use System Settings > Privacy & Security > Open Anyway, then confirm Open.

Includes the pickup-marker collision fix, updated sea/fire audio levels,
and Yuval Bashan as the first credit. Retesting these changes on Mac is pending.
Credits.txt and Audio-Credits.txt contain credits and audio attribution.
''', encoding='utf-8')

def arch(path):
    with path.open('rb') as f:
        h = f.read(8)
        if len(h) < 8: return []
        names = {0x01000007: 'x86_64', 0x0100000c: 'arm64'}
        if h[:4] in (b'\xca\xfe\xba\xbe', b'\xca\xfe\xba\xbf'):
            n = struct.unpack('>I', h[4:])[0]
            size = 32 if h[3] == 0xbf else 20
            return [names.get(struct.unpack('>I', f.read(size)[:4])[0], 'unknown') for _ in range(n)]
        if h[:4] in (b'\xcf\xfa\xed\xfe', b'\xce\xfa\xed\xfe'):
            return [names.get(struct.unpack('<I', h[4:])[0], 'unknown')]
        return []

native = {p.relative_to(mac).as_posix(): a for p in app.rglob('*') if p.is_file() and (a := arch(p))}
assert native and all(set(a) == {'x86_64', 'arm64'} for a in native.values()), native
archive = mac / 'PiratesOfTheMediterranean-MacUniversal.zip'
with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for p in sorted(app.rglob('*')) + [mac / name for name in ['README.txt', 'Credits.txt', 'Audio-Credits.txt']]:
        if not p.is_file(): continue
        name = p.relative_to(mac).as_posix()
        info = zipfile.ZipInfo.from_file(p, name)
        info.create_system = 3
        info.external_attr = (stat.S_IFREG | (0o755 if name in native else 0o644)) << 16
        info.compress_type = zipfile.ZIP_DEFLATED
        with p.open('rb') as source, z.open(info, 'w') as target:
            shutil.copyfileobj(source, target)
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert all(z.getinfo(name).external_attr >> 16 & 0o111 == 0o111 for name in native)
with archive.open('rb') as f: digest = hashlib.file_digest(f, 'sha256').hexdigest()
result = dict(native_architectures=native, zip_bytes=archive.stat().st_size,
              sha256=digest, zip_crc_verified=True, unix_executable_permissions=True)
(mac / 'package-validation.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
