#!/usr/bin/env python3
"""
Assembles Assets/_Game/ArtNew from the CC0 Kenney packs in kenney_images/.
Re-runnable: every output is derived, nothing is hand-edited.
  python3 tools/build_art.py
"""
from PIL import Image, ImageDraw, ImageFilter
import os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
K = os.path.join(ROOT, 'kenney_images')
OUT = os.path.join(ROOT, 'Assets', '_Game', 'Art')

PUZ = os.path.join(K, 'kenney_puzzle-pack-2', 'PNG')
PLAT = os.path.join(K, 'kenney_platformer-pack-remastered', 'PNG')
UI = os.path.join(K, 'kenney_ui-pack', 'PNG')
PART = os.path.join(K, 'kenney_particle-pack', 'PNG (Transparent)')

COLORS = {'red': 'Red', 'green': 'Green', 'blue': 'Blue', 'yellow': 'Yellow'}

def load(p): return Image.open(p).convert('RGBA')
def save(im, rel):
    p = os.path.join(OUT, rel); os.makedirs(os.path.dirname(p), exist_ok=True); im.save(p); print('  ', rel, im.size)
def trim(im, pad=2):
    """Crop transparent margins (Kenney items sit inside a 128px frame with empty borders); keep square."""
    box = im.getchannel('A').getbbox()
    if not box: return im
    l, t, r, b = box; w, h = r - l, b - t; side = max(w, h) + 2 * pad
    out = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    out.alpha_composite(im.crop(box), ((side - w) // 2, (side - h) // 2)); return out

def fit(im, size):
    im = im.copy(); im.thumbnail((size, size), Image.LANCZOS); return im
def center_paste(base, top, dy=0):
    base = base.copy()
    x = (base.width - top.width) // 2; y = (base.height - top.height) // 2 + dy
    base.alpha_composite(top, (x, y)); return base

# ---------- derived drawings ----------
def draw_rocket(w=72, h=150):
    """Flat-style rocket pointing UP in Kenney's palette."""
    s = 4; W, H = w * s, h * s                        # draw big, downsample for smooth edges
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    body = (0.22 * W, 0.26 * H, 0.78 * W, 0.86 * H)
    red, dark, grey, light, blue = (231, 76, 60, 255), (160, 50, 40, 255), (220, 222, 225, 255), (245, 246, 247, 255), (52, 152, 219, 255)
    # fins
    d.polygon([(0.22 * W, 0.62 * H), (0.02 * W, 0.92 * H), (0.22 * W, 0.86 * H)], fill=red)
    d.polygon([(0.78 * W, 0.62 * H), (0.98 * W, 0.92 * H), (0.78 * W, 0.86 * H)], fill=red)
    # body + nose
    d.rounded_rectangle(body, radius=0.18 * W, fill=light, outline=grey, width=s * 2)
    d.polygon([(0.22 * W, 0.30 * H), (0.5 * W, 0.02 * H), (0.78 * W, 0.30 * H)], fill=red)
    # stripe + window + nozzle
    d.rectangle((0.22 * W, 0.46 * H, 0.78 * W, 0.52 * H), fill=red)
    d.ellipse((0.36 * W, 0.56 * H, 0.64 * W, 0.56 * H + 0.28 * W), fill=blue, outline=grey, width=s * 2)
    d.polygon([(0.34 * W, 0.86 * H), (0.66 * W, 0.86 * H), (0.6 * W, 0.98 * H), (0.4 * W, 0.98 * H)], fill=dark)
    return im.resize((w, h), Image.LANCZOS)

def gradient_bg(w=1080, h=1920, top=(44, 82, 150), bottom=(16, 22, 46)):
    """Vertical gradient with a soft glow where the title sits and faint diagonal bands for texture."""
    import math
    im = Image.new('RGBA', (w, h)); px = im.load()
    gx, gy, gr = w * 0.5, h * 0.30, w * 0.75
    for y in range(h):
        t = y / (h - 1)
        base = [top[i] * (1 - t) + bottom[i] * t for i in range(3)]
        for x in range(w):
            d = math.hypot(x - gx, y - gy) / gr
            glow = max(0.0, 1 - d) ** 2 * 70                       # warm light behind the title
            band = 6 if ((x + y) // 90) % 2 == 0 else 0            # very subtle diagonal stripes
            px[x, y] = (int(min(255, base[0] + glow + band)), int(min(255, base[1] + glow * 0.9 + band)), int(min(255, base[2] + glow * 0.6 + band)), 255)
    return im

def crops(im, n, size):
    """n random-ish square chunks of a sprite, used as debris particles."""
    out = []; W, H = im.size
    pts = [(0.15, 0.15), (0.6, 0.2), (0.3, 0.6), (0.65, 0.65), (0.4, 0.4), (0.1, 0.55), (0.7, 0.4), (0.25, 0.3), (0.55, 0.5), (0.45, 0.7)]
    for i in range(n):
        fx, fy = pts[i % len(pts)]; cw = int(W * 0.3)
        x, y = int(fx * (W - cw)), int(fy * (H - cw))
        out.append(fit(im.crop((x, y, x + cw, y + cw)), size))
    return out

def main():
    print('->', OUT)
    # ---- cubes: beveled square tiles, hint states with an icon on top ----
    rocket_icon = draw_rocket(34, 70)
    bomb = trim(load(os.path.join(PLAT, 'Tiles', 'bomb.png')))
    for key, name in COLORS.items():
        tile = load(os.path.join(PUZ, f'Tiles {key}', f'tile{name}_01.png'))
        save(tile, f'Cubes/DefaultState/{key}.png')
        save(center_paste(tile, rocket_icon), f'Cubes/RocketState/{key}_rocket.png')
        save(center_paste(tile, fit(bomb, int(tile.width * 0.55))), f'Cubes/BombState/{key}_bomb.png')
        star = load(os.path.join(PUZ, f'Tiles {key}', f'tile{name}_08.png'))
        save(fit(star, 40), f'Cubes/Particles/particle_{key}.png')

    # ---- rocket: generated, vertical + horizontal, and the four flying halves ----
    v = draw_rocket(); save(v, 'SpecialItems/Rocket/vertical_rocket.png')
    h = v.rotate(-90, expand=True); save(h, 'SpecialItems/Rocket/horizontal_rocket.png')
    half = v.height // 2
    save(v.crop((0, 0, v.width, half)), 'SpecialItems/Rocket/vertical_rocket_part_top.png')
    save(v.crop((0, half, v.width, v.height)), 'SpecialItems/Rocket/vertical_rocket_part_bottom.png')
    halfw = h.width // 2
    save(h.crop((0, 0, halfw, h.height)), 'SpecialItems/Rocket/horizontal_rocket_part_left.png')
    save(h.crop((halfw, 0, h.width, h.height)), 'SpecialItems/Rocket/horizontal_rocket_part_right.png')
    save(fit(load(os.path.join(PART, 'smoke_05.png')), 128), 'SpecialItems/Rocket/Particles/particle_smoke.png')
    save(fit(load(os.path.join(PART, 'star_06.png')), 96), 'SpecialItems/Rocket/Particles/particle_star.png')

    # ---- Bomb ----
    save(bomb, 'SpecialItems/Bomb/Bomb.png')
    save(fit(load(os.path.join(PART, 'smoke_03.png')), 96), 'SpecialItems/Bomb/Particles/particle_bomb_01.png')
    save(fit(load(os.path.join(PART, 'smoke_08.png')), 96), 'SpecialItems/Bomb/Particles/particle_bomb_02.png')

    # ---- obstacles ----
    rock = trim(load(os.path.join(PLAT, 'Tiles', 'rock.png'))); save(rock, 'Obstacles/Rock/rock.png')
    for i, c in enumerate(crops(rock, 3, 36), 1): save(c, f'Obstacles/Rock/Particles/particle_rock_0{i}.png')

    crate = trim(load(os.path.join(PLAT, 'Tiles', 'boxCrate.png'))); cracked = trim(load(os.path.join(PLAT, 'Tiles', 'boxCrate_single.png')))
    save(crate, 'Obstacles/Crate/crate_01.png'); save(cracked, 'Obstacles/Crate/crate_02.png')
    for i, c in enumerate(crops(crate, 3, 36), 1): save(c, f'Obstacles/Crate/Particles/particle_crate_0{i}.png')

    doors = trim(load(os.path.join(PLAT, 'Tiles', 'boxCoin.png'))); bg = trim(load(os.path.join(PLAT, 'Tiles', 'boxCoin_disabled.png')))
    coin = trim(load(os.path.join(PLAT, 'Items', 'coinGold.png')))
    save(doors, 'Obstacles/Chest/ChestDoors.png'); save(bg, 'Obstacles/Chest/ChestBg.png'); save(coin, 'Obstacles/Chest/Coin.png')
    for i, c in enumerate(crops(bg, 5, 36), 1): save(c, f'Obstacles/Chest/Particles/BaseParticles/chest_particle_0{i}.png')
    for i, c in enumerate(crops(doors, 4, 36), 1): save(c, f'Obstacles/Chest/Particles/DoorParticles/chest_door_particle_0{i}.png')

    # ---- UI (Kenney UI pack, "Double" = 2x resolution) ----
    def ui(color, name): return load(os.path.join(UI, color, 'Double', name))
    save(ui('Grey', 'button_square_border.png'), 'UI/Gameplay/Popup/popup_base.png')
    save(ui('Blue', 'button_rectangle_depth_gradient.png'), 'UI/Gameplay/Popup/popup_ribbon.png')
    close = ui('Red', 'button_round_depth_gradient.png'); cross = ui('Grey', 'icon_outline_cross.png')
    save(center_paste(close, fit(cross, int(close.width * 0.5)), dy=-3), 'UI/Gameplay/Popup/close_button.png')
    save(trim(ui('Yellow', 'star.png')), 'UI/Gameplay/Celebration/star.png')
    save(fit(load(os.path.join(PART, 'star_06.png')), 128), 'UI/Gameplay/Celebration/Particles/additive_particle_star.png')
    save(trim(ui('Green', 'icon_checkmark.png')), 'UI/Gameplay/Top/goal_check.png')
    save(ui('Blue', 'button_rectangle_depth_flat.png'), 'UI/Gameplay/Top/panel.png')          # top bar background (9-slice)
    save(ui('Grey', 'button_square_flat.png'), 'UI/Gameplay/Top/slot.png')                   # goal / moves boxes (9-slice)
    save(ui('Grey', 'button_square_border.png'), 'UI/Gameplay/grid_background.png')           # board frame (9-slice)
    save(ui('Green', 'button_rectangle_depth_gradient.png'), 'UI/Menu/button_full.png')
    save(ui('Green', 'button_rectangle_gradient.png'), 'UI/Menu/button_frame_full.png')       # pressed look
    save(gradient_bg(), 'Menu/background.png')
    print('done')

if __name__ == '__main__':
    main()
