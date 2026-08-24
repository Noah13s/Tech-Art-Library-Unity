# Bullet

A data-driven projectile prototype covering ammunition compatibility, magazines, rigidbody flight, impacts, simplified penetration, and optional fragmentation.

## Open the demo

Open [`TestRange.unity`](TestRange.unity), enter Play Mode, select the weapon object, and press **Fire** in its Inspector. The demo intentionally exposes firing through a custom Inspector button rather than a gameplay input binding.

## Data flow

1. [`Weapon`](Weapon.cs) checks that its magazine contains compatible ammunition.
2. [`Magazine`](Magazine.cs) decrements its current ammunition count.
3. A prefab is created from the selected [`BulletData`](BulletData.cs) asset.
4. [`BulletObject`](BulletObject.cs) applies muzzle velocity, gravity, continuous collision detection, drag, impact behavior, penetration, and fragmentation.
5. [`BulletMaterial`](BulletMaterial.cs) supplies simplified target properties used during impact resolution.

The folder includes example `.22 LR`, `9 mm`, and `120 mm` ammunition assets plus a configured [`BulletPrefab.prefab`](BulletPrefab.prefab).

## Creating ammunition

Create an asset through **Assets > Create > Weapons > Bullet Data**, then configure:

- muzzle speed and projectile lifetime;
- mass and ballistic coefficient;
- bullet prefab and impact effect;
- fragmentation behavior.

Add the asset to a magazine and to the weapon's compatible-ammunition list.

## Scope

The penetration and fragmentation model is deliberately simplified and is not a validated real-world ballistics solver. Tune it for the visual and gameplay scale of your project.
