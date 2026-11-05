p = 'Assets/Scripts/UI/CombatHudCanvasUI.cs'
t = open(p, encoding='utf-8').read()

# 1. Old serialized scene buttons keep minWidth=280/320 -> force it down so 155 wins
n = t.count('le.preferredWidth = 155f;')
t = t.replace('le.preferredWidth = 155f;', 'le.preferredWidth = 155f;\n                le.minWidth = 0f;\n                le.flexibleWidth = 0f;')

# 2. avoid duplicate LayoutElement in BuildDynamicHud (CreateBtn already adds one)
for v in ['leTR', 'leRM', 'leRD']:
    t = t.replace(f'LayoutElement {v} = ', f'LayoutElement {v} = ', 1)
for b, v in [('btnTearRift', 'leTR'), ('btnRandomizeMap', 'leRM'), ('btnOpenRiftDispatch', 'leRD')]:
    t = t.replace(f'LayoutElement {v} = {b}.gameObject.AddComponent<LayoutElement>();',
                  f'LayoutElement {v} = {b}.GetComponent<LayoutElement>() ?? {b}.gameObject.AddComponent<LayoutElement>();')
    t = t.replace(f'{v}.preferredWidth = 155f;', f'{v}.preferredWidth = 155f;\n            {v}.minWidth = 0f;')

open(p, 'w', encoding='utf-8').write(t)
print('patched', n)
