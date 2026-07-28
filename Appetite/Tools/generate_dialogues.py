#!/usr/bin/env python3
"""
Generate all DialogueNode .asset and .meta files for the Appetite game.
Run this script from the project root (where Assets/ lives).
"""

import os
import uuid
import hashlib

BASE_DIR = "Assets/_Game/Data/Dialogues"
SCRIPT_GUID = "cd630ef5f4b574721969adf5b45eb283"  # DialogueNode.cs

def make_guid(name):
    """Generate a deterministic GUID from a name."""
    h = hashlib.md5(name.encode()).hexdigest()
    return h

def make_meta(guid):
    return f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""

def make_asset(name, text, speaker, options=None, next_node_guid=None,
               end_action="None", end_action_scene=""):
    """Generate a .asset file content."""
    end_action_map = {"None": 0, "LoadScene": 1, "ReturnToPrevious": 2}
    ea = end_action_map.get(end_action, 0)

    # Build options YAML
    if options:
        opt_lines = []
        for opt in options:
            next_guid = opt.get("next_guid", "")
            hc = opt.get("hungerChange", 0)
            opt_lines.append(f"  - optionText: \"{opt['text']}\"")
            if next_guid:
                opt_lines.append(f"    nextNode: {{fileID: 11400000, guid: {next_guid}, type: 2}}")
            else:
                opt_lines.append(f"    nextNode: {{fileID: 0}}")
            opt_lines.append(f"    hungerChange: {hc}")
        options_yaml = "\n".join(opt_lines)
    else:
        options_yaml = "[]"

    # Build nextNode reference
    if next_node_guid:
        next_node_ref = f"{{fileID: 11400000, guid: {next_node_guid}, type: 2}}"
    else:
        next_node_ref = "{fileID: 0}"

    # Escaped text
    escaped_text = text.replace('\\', '\\\\').replace('"', '\\"')

    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
  text: "{escaped_text}"
  speakerName: "{speaker}"
  speakerIcon: {{fileID: 0}}
  options: {options_yaml}
  isAutoNext: 1
  nextNode: {next_node_ref}
  endAction: {ea}
  endActionSceneName: {end_action_scene}
"""

# ============================================================
# DIALOGUE DEFINITIONS
# ============================================================
# Each entry: (key, name, speaker, text, [options], next_key, end_action, end_scene)
# options: list of (text, next_key, hungerChange)

def define_all_dialogues():
    dialogues = {}

    # =====================
    # HOSPITAL - Nurse/Doctor Sequence (linear)
    # =====================
    hospital_nurse = [
        ("H1_Nurse1", "H1_Nurse1", "护士",
         "丽姐，三号床就是你们说的那个人？",
         None, "H2_HeadNurse1"),

        ("H2_HeadNurse1", "H2_HeadNurse1", "护士长",
         "是啊，又因为营养含量不足进来了，前几个月刚来过，现在一年都要来个两三次了。",
         None, "H3_Nurse2"),

        ("H3_Nurse2", "H3_Nurse2", "护士",
         "这都两个周期的光照治疗了，怎么营养水平还这么低？",
         None, "H4_HeadNurse2"),

        ("H4_HeadNurse2", "H4_HeadNurse2", "护士长",
         "我看，两个周期光照治疗后还没恢复到正常水平，你快去找孙主任，看要不要再开一剂营养针。不过这个营养水平还是有上升的，估计再来一个周期也差不多了。",
         None, "H5_Nurse3"),

        ("H5_Nurse3", "H5_Nurse3", "护士",
         "好，孙主任刚查完房。",
         None, "H6_HeadNurse3"),

        ("H6_HeadNurse3", "H6_HeadNurse3", "护士长",
         "真是的，身体这么弱还不注意，总把自己送进医院。",
         None, "H7_WakeUp"),

        ("H7_WakeUp", "H7_WakeUp", "旁白",
         "你缓缓睁开眼睛，能听见周围的声音，但说不了话，四肢像是打了麻药一般无法移动。",
         None, "H8_SunDirector"),

        ("H8_SunDirector", "H8_SunDirector", "孙主任",
         "三号床营养持续未达标，开一只营养针，再来一个光照治疗，如果还不够再叫我。",
         None, "H9_Clipboard"),

        ("H9_Clipboard", "H9_Clipboard", "旁白",
         "医生拿着一个夹板在上面写字，夹板的底部悬在你的眼前。枫科林蒂医院六个字环着医院的标志：一根权杖上盘着两条蛇。医生写完字之后，顺手把夹板放在你的床头柜上。护士端着托盘进来了，上面放了一只注射器和一个小绿瓶，上面写着Luminal Nutrient Solution，LNS。",
         None, "H10_NurseInjection"),

        ("H10_NurseInjection", "H10_NurseInjection", "护士",
         "您好，我给您注射一下营养针。",
         None, "H11_InjectionDetail"),

        ("H11_InjectionDetail", "H11_InjectionDetail", "旁白",
         "她把你的右胳膊拿过去掰直，用两根沾了碘伏的棉签在你胳膊肘的内侧画圈消毒，动作很迅速。然后她拿起注射器，从小绿瓶中抽出来液体，针尖朝上，轻轻弹了弹针筒，将空气聚集到顶部，再缓缓推动活塞，直到在针尖逼出一滴药液。她的手指在你静脉的位置稍微比划了一下，然后径直把针扎了进去。你深吸了一口气，她不为所动。将液体注射进去之后，她抽出棉花，给你摁住。",
         None, "H12_LightTherapy"),

        ("H12_LightTherapy", "H12_LightTherapy", "护士",
         "您用身体压一下吧，我给您做光照治疗。",
         None, "H13_LimbPower"),

        ("H13_LimbPower", "H13_LimbPower", "旁白",
         "她帮你把胳膊叠在胸前，用胸压着棉花。她转到你的左手边，将电子阳光灯覆在你的左手小臂上。你终于感到四肢有了点力量，动了动小腿。",
         None, "H14_LightInfo"),

        ("H14_LightInfo", "H14_LightInfo", "旁白",
         "光照治疗是给光核芯片增加人造灯光，对于光照转化不足的病人，是目前唯一的主流治疗手段。",
         None, "H15_Note"),

        ("H15_Note", "H15_Note", "旁白",
         "你看右胳膊已经不再流血，抬手把棉花扔到垃圾桶里，然后撑着自己的上身，稍稍坐起来一点。孙主任刚刚把他的夹板放在你的床头，你扭头过去看他都写了什么。病历上的表述你都熟悉，但是末尾有一行龙飞凤舞的批注：「已产生抗药性，建议先加大用量」。",
         None, "H16_DirectorTakes"),

        ("H16_DirectorTakes", "H16_DirectorTakes", "旁白",
         "这时，孙主任从外面进来。他看到你在看夹板，直接把夹板拿走了，没朝你这边看一眼。",
         None, "H17_Recovery"),

        ("H17_Recovery", "H17_Recovery", "旁白",
         "你感到光照治疗和营养针在你的身上起了一点作用，你终于觉得你有坐起来的力量了。",
         None, "H18_NurseFinal"),

        ("H18_NurseFinal", "H18_NurseFinal", "护士",
         "你的体内营养含量已经够了啊，一会儿让孙主任来再看看。单子我都给您放在这儿了。",
         None, "HC1_Flash"),  # 直接串联到黑猫对话
    ]

    for item in hospital_nurse:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "Hospital"
        }

    # =====================
    # BLACK CAT - Hospital (linear, after drawer interaction)
    # =====================
    black_cat = [
        ("HC1_Flash", "HC1_Flash", "旁白",
         "拉开抽屉时，灯突然闪了一下。你抬起头，一只黑猫正静静地坐在窗台上，琥珀色的眼睛直直盯着你。",
         None, "HC2_Surprised"),

        ("HC2_Surprised", "HC2_Surprised", "你",
         "！！！",
         None, "HC3_CatInHospital"),

        ("HC3_CatInHospital", "HC3_CatInHospital", "你",
         "医院里怎么有猫！？",
         None, "HC4_CatWakeUp"),

        ("HC4_CatWakeUp", "HC4_CatWakeUp", "黑猫",
         "你终于醒了。",
         None, "HC5_Delusional"),

        ("HC5_Delusional", "HC5_Delusional", "你",
         "我难道终于病得神志不清了？猫怎么在和我说话？？",
         None, "HC6_CatNotPoint"),

        ("HC6_CatNotPoint", "HC6_CatNotPoint", "黑猫",
         "不是重点。重点是，你快饿死了。",
         None, "HC7_CallNurse"),

        ("HC7_CallNurse", "HC7_CallNurse", "你",
         "你在说什么？护士，这里有只猫进来了！",
         None, "HC8_CatNoOneSees"),

        ("HC8_CatNoOneSees", "HC8_CatNoOneSees", "黑猫",
         "别叫了，别人看不到我。",
         None, "HC9_Crazy"),

        ("HC9_Crazy", "HC9_Crazy", "你",
         "……那我在这里大喊大叫，别人岂不是会把我当疯子？",
         None, "HC10_CatQuiet"),

        ("HC10_CatQuiet", "HC10_CatQuiet", "黑猫",
         "所以你最好小点声。",
         None, "HC11_CatTruth"),

        ("HC11_CatTruth", "HC11_CatTruth", "黑猫",
         "医院说你的病是光照转化率低下，但是你的身体其实有别的问题。",
         None, "HC12_CatHunger"),

        ("HC12_CatHunger", "HC12_CatHunger", "黑猫",
         "你现在正在经历的感觉，就是饥饿。",
         None, "HC13_YouHunger"),

        ("HC13_YouHunger", "HC13_YouHunger", "你",
         "饥……饿？",
         None, "HC14_CatExplain"),

        ("HC14_CatExplain", "HC14_CatExplain", "黑猫",
         "你会懂的。这是一种古老的感觉。虽然你现在的营养水平已经回归正常了，但是你还感到饥饿。如果你一直饥饿，你将会进入另一世界。你最好赶紧找到拯救自己的方式，不然谁都帮不了你了。",
         None, "HC15_Confused"),

        ("HC15_Confused", "HC15_Confused", "你",
         "？你在说什么呢",
         None, "HC16_CatSilence"),

        ("HC16_CatSilence", "HC16_CatSilence", "黑猫",
         "………",
         None, None, "LoadScene", "ExplorationScene"),
    ]

    for item in black_cat:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "BlackCat"
        }

    # =====================
    # COLLEAGUE - Street scene (with 3 options)
    # =====================
    colleague = [
        ("C1_Meet", "C1_Meet", "旁白",
         "你朝家的方向走去，你已经很熟悉这条路了。迎面来了个熟悉的身影，是你的前同事，你上前和ta打了个招呼。",
         None, "C2_ColleagueGreet"),

        ("C2_ColleagueGreet", "C2_ColleagueGreet", "前同事",
         "哦，你好啊。你最近怎么样？",
         [
             ("刚刚出院", "C3a_JustLeft", 0),
             ("老毛病了", "C3b_OldProblem", 0),
             ("……", "C3c_DotDotDot", 0),
         ], None),

        ("C3a_JustLeft", "C3a_JustLeft", "你",
         "刚刚出院。",
         None, "C4_ColleagueResponse"),

        ("C3b_OldProblem", "C3b_OldProblem", "你",
         "老毛病了。",
         None, "C4_ColleagueResponse"),

        ("C3c_DotDotDot", "C3c_DotDotDot", "你",
         "……",
         None, "C4_ColleagueResponse"),

        ("C4_ColleagueResponse", "C4_ColleagueResponse", "前同事",
         "又病了？你这个身体真是，一点小毛病就撑不住……唉，不在这地方干了也好。我每天为了那点加班费都十一点才下班。",
         None, "C5_YouRest"),

        ("C5_YouRest", "C5_YouRest", "你",
         "啊，你也得多注意休息啊。",
         None, "C6_ColleagueLeave"),

        ("C6_ColleagueLeave", "C6_ColleagueLeave", "前同事",
         "趁年轻得多干活儿啊，你那个老毛病得好好看看，老这样怎么行？我先走了。",
         None, "C7_NarratorLeave"),

        ("C7_NarratorLeave", "C7_NarratorLeave", "旁白",
         "你不得不承认，ta确实不愿和你多说话。告别前同事后，你闻到了一种奇特的香味。",
         None, "B1_Shopkeeper"),  # 串联到面包店
    ]

    for item in colleague:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "Colleague"
        }

    # =====================
    # BAKERY - Shop encounter (linear)
    # =====================
    bakery = [
        ("B1_Shopkeeper", "B1_Shopkeeper", "店主",
         "你喜欢这款香薰的味道吗？",
         None, "B2_YouLike"),

        ("B2_YouLike", "B2_YouLike", "你",
         "我喜欢。它让我想起了什么，让我觉得我好像需要些什么。",
         None, "B3_ShopkeeperGift"),

        ("B3_ShopkeeperGift", "B3_ShopkeeperGift", "店主",
         "从来没有人和我一样喜欢这款香薰的味道，你是第一个。我把这个面包香薰送给你吧，它总是卖不出去的那一款，难得你能喜欢它。",
         None, "A1_Auntie"),  # 串联到居委会
    ]

    for item in bakery:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "Bakery"
        }

    # =====================
    # AUNTIE - Community auntie (linear)
    # =====================
    auntie = [
        ("A1_Auntie", "A1_Auntie", "居委会阿姨",
         "小张！你出院了？现在好点了吗？那天你突然就在你们家楼下晕倒，吓得我赶紧把你送到医院去了，可把我担心死了。",
         None, "A2_YouBetter"),

        ("A2_YouBetter", "A2_YouBetter", "你",
         "现在好点了。",
         None, "A3_AuntieAdvice"),

        ("A3_AuntieAdvice", "A3_AuntieAdvice", "居委会阿姨",
         "小张，你得注意身体啊，好好休息。你们这代人的身子板儿太弱了，比不上之前，可千万别把自己累坏了。",
         None, "A4_YouThanks"),

        ("A4_YouThanks", "A4_YouThanks", "你",
         "谢谢阿姨，也辛苦您当时送我到医院了。",
         None, "A5_AuntieService"),

        ("A5_AuntieService", "A5_AuntieService", "居委会阿姨",
         "哎呀，辛苦什么，我不就是给你们服务的嘛。",
         None, "PC1_HungerSymptoms"),  # 串联到电脑场景
    ]

    for item in auntie:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "Auntie"
        }

    # =====================
    # COMPUTER - Home scene (linear, ends in faint → SpiritWorld)
    # =====================
    computer = [
        ("PC1_HungerSymptoms", "PC1_HungerSymptoms", "旁白",
         "饱腹值逐渐下降，屏幕边缘模糊抖动，画面变暗。你的肚子发出「咕噜咕噜」的声响，身上冒出一阵阵冷汗。胃里有一种空荡荡的感受，双手不受控地发抖。",
         None, "PC2_Thought"),

        ("PC2_Thought", "PC2_Thought", "你",
         "好难受。从窗户跳进来的那只黑猫、那只我能听懂它叫声的黑猫，它告诉我的是什么……？饥饿、是这个名字没错。我从来没有听说过。什么叫只有我会……小小鹅今天上线了吗，我去问问她吧。",
         None, "PC3_Computer"),

        ("PC3_Computer", "PC3_Computer", "旁白",
         "你打开电脑。屏幕发出的光在昏暗房间里显得尤为刺眼。电脑桌面很干净，几个排列得井井有条的图标。你点开和白鹅的聊天框，上一条消息停留在你晕倒之前……或许是小小鹅先前发的消息你没有回复，对方知道他会晕倒于是说了些关心的话。",
         None, "PC4_Typing"),

        ("PC4_Typing", "PC4_Typing", "你",
         "我没事，和之前一样。社区的阿姨把我送到医院的。",
         None, "PC5_Typing2"),

        ("PC5_Typing2", "PC5_Typing2", "旁白",
         "画面闪烁抖动。胃中空洞感带来剧烈眩晕。你强忍住不适继续打字。",
         None, "PC6_Typing3"),

        ("PC6_Typing3", "PC6_Typing3", "你",
         "但发生了好多事，我几乎以为自己在做梦。小小鹅，你听说过饥饿吗？你也知道我的事，我……",
         None, "PC7_Knock"),

        ("PC7_Knock", "PC7_Knock", "旁白",
         "门外缓缓的脚步声靠近。敲门声响起。眩晕感无法忍受，视线里电脑发亮的屏幕渐渐溶解进一片模糊的黑暗，四肢仿佛就此离你而去。门外落入寂静，脚步声渐远。",
         None, None, "LoadScene", "Fight"),
    ]

    for item in computer:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "Computer"
        }

    # =====================
    # SPIRIT WORLD - Black Cat Intro (linear)
    # =====================
    spirit_cat = [
        ("SC1_CatDarkness", "SC1_CatDarkness", "旁白",
         "黑猫从黑暗中出现。",
         None, "SC2_CatIntro"),

        ("SC2_CatIntro", "SC2_CatIntro", "黑猫",
         "这里是饿的人来的地方。欢迎来到精神世界。在这里，你的饥饿会指引你。去吧，前面有什么在等着你。",
         None, None, "None", ""),
    ]

    for item in spirit_cat:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "SpiritCat"
        }

    # =====================
    # DOG - Expanded dialogue (with 3 options, each with multiple follow-up nodes)
    # =====================
    dog = [
        # Entry node - Dog1 (overwrite existing)
        ("Dog1", "Dog1", "黑狗",
         "汪，你还好吗？这里是饿的人来的地方。你一定饿了很久吧汪。",
         [
             ("吃下面包", "DogEat1", 10),
             ("不吃，追问它", "DogRefuse1", 0),
             ("你是谁？", "DogWho1", 0),
         ], None),

        # === Eat Branch ===
        ("DogEat1", "DogEat1", "旁白",
         "黑狗用鼻子将羊皮纸包裹的散发着香气与热气的面包推到你的面前。你咬了一口面包。面包很硬，嚼起来让你感到腮帮子累。它吃起来远没有闻起来那么好，但面团的味道还是让你觉得很新奇。",
         None, "DogEat2"),

        ("DogEat2", "DogEat2", "黑狗",
         "怎么样，味道好吧！",
         None, "DogEat3"),

        ("DogEat3", "DogEat3", "你",
         "……我不讨厌。",
         None, "DogEat4"),

        ("DogEat4", "DogEat4", "旁白",
         "你感到自己胃中前所未有的温暖舒展的感受，低下头看了看手中的被啃了一小口的面包。其实是很喜欢。",
         None, "DogEat5"),

        ("DogEat5", "DogEat5", "黑狗",
         "管用吧，那就好！以后要记得吃汪。",
         None, "DogWakeUp"),

        # === Refuse Branch ===
        ("DogRefuse1", "DogRefuse1", "你",
         "我不饿。",
         None, "DogRefuse2"),

        ("DogRefuse2", "DogRefuse2", "黑狗",
         "你骗人汪。你看起来不是不饿的人会有的样子。",
         None, "DogRefuse3"),

        ("DogRefuse3", "DogRefuse3", "你",
         "……不饿的人该是什么样子？",
         None, "DogRefuse4"),

        ("DogRefuse4", "DogRefuse4", "旁白",
         "黑狗没有立刻回答，只将纸包又往前推了推，退回原位蹲坐下来。",
         None, "DogRefuse5"),

        ("DogRefuse5", "DogRefuse5", "黑狗",
         "……不饿的人不会这样痛苦汪。",
         None, "DogRefuse6"),

        ("DogRefuse6", "DogRefuse6", "你",
         "……你为什么要帮我？",
         None, "DogRefuse7"),

        ("DogRefuse7", "DogRefuse7", "黑狗",
         "饥饿很难受汪。我不想看你这样难受。",
         None, "DogRefuse8"),

        ("DogRefuse8", "DogRefuse8", "你",
         "……谢谢，但是先不用了。",
         None, "DogRefuse9"),

        ("DogRefuse9", "DogRefuse9", "旁白",
         "你无法接受把什么东西塞到嘴里，所以还是没有选择吃。不过你把面包收起来了，以后再说吧。",
         None, "DogWakeUp"),

        # === Who Branch ===
        ("DogWho1", "DogWho1", "你",
         "我不认识你。你是谁？",
         None, "DogWho2"),

        ("DogWho2", "DogWho2", "黑狗",
         "我只是一只路过的狗，汪。",
         None, "DogWho3"),

        ("DogWho3", "DogWho3", "你",
         "路过？",
         None, "DogWho4"),

        ("DogWho4", "DogWho4", "黑狗",
         "是的，汪。我感知到了你散发出的饥饿的气味。",
         None, "DogWho5"),

        ("DogWho5", "DogWho5", "你",
         "难道饥饿的人有特殊的气味吗？我是特殊的那一个吗？",
         None, "DogWho6"),

        ("DogWho6", "DogWho6", "黑狗",
         "是的，所以你更要照顾好自己。你会需要它的汪。",
         None, "DogWho7"),

        ("DogWho7", "DogWho7", "旁白",
         "它把纸包放到了你的背包里。",
         None, "DogWakeUp"),

        # Wake Up (all branches converge here)
        ("DogWakeUp", "DogWakeUp", "旁白",
         "一股温暖的力量将你拉回现实……你缓缓睁开了眼睛。",
         None, None, "LoadScene", "ExplorationScene"),
    ]

    for item in dog:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "Dog"
        }

    # =====================
    # POST-SPIRITWORLD - Awakening in MainWorld (linear, ends → MainMenu)
    # =====================
    awakening = [
        ("PW1_WakeWithCat", "PW1_WakeWithCat", "旁白",
         "你从精神世界中醒来，视野中第一个出现的是黑猫的脸。它正蹲在你的胸口上，琥珀色的眼睛静静看着你。",
         None, "PW2_CatWake"),

        ("PW2_CatWake", "PW2_CatWake", "黑猫",
         "你醒了。",
         None, "PW3_BreadTalk"),

        ("PW3_BreadTalk", "PW3_BreadTalk", "你",
         "刚刚那条黑狗……它给了我面包……",
         None, "PW4_CatBread"),

        ("PW4_CatBread", "PW4_CatBread", "黑猫",
         "是的。现在你接受了吗？面包就是面包，是可以放进嘴里咀嚼、吞咽的面包，这才是它存在的意义，而不是摆在那里，做一个散发气味的「香薰」。",
         None, "PW5_DogGone"),

        ("PW5_DogGone", "PW5_DogGone", "你",
         "或许吧。那刚刚那条黑狗，它就这样路过我，然后又消失了吗？",
         None, "PW6_CatTrace"),

        ("PW6_CatTrace", "PW6_CatTrace", "黑猫",
         "只要你记得它，这个世界里就总会有它存在的印迹。",
         None, "PW7_Footprints"),

        ("PW7_Footprints", "PW7_Footprints", "旁白",
         "你顺着黑猫的目光望去，发现地上出现一排黑狗的脚印。脚印排列成一条路线，延伸向远方。",
         None, "PW8_Follow"),

        ("PW8_Follow", "PW8_Follow", "旁白",
         "你顺着脚印穿过街道，走过香薰店，来到店后面一片早已荒废的小花园。杂草几乎淹没了一切，你拨开花丛，看到里面静静长着几颗深蓝色的浆果。",
         None, "PW9_CatEat"),

        ("PW9_CatEat", "PW9_CatEat", "黑猫",
         "吃吧。",
         [("吃下去", "PW10_EatBerries", 20)], None),

        ("PW10_EatBerries", "PW10_EatBerries", "旁白",
         "你咬下浆果。整个世界失去颜色，杂草渐渐枯萎、缩小，画面变为没有边界的纯白空间，只有你孤零零地站着。远处传来孩子的笑声——「快回家啦！」「再不吃就凉啦！」笑声逐渐变得尖锐，转变为耳鸣，然后突然停止。仿佛从很远很远的地方传来一句，声音很陌生：「不要忘记饥饿。」",
         None, None, "LoadScene", "MainMenu"),
    ]

    for item in awakening:
        key, name, speaker, text, opts, next_key = item[:6]
        end_action = item[6] if len(item) > 6 else "None"
        end_scene = item[7] if len(item) > 7 else ""
        dialogues[key] = {
            "name": name, "speaker": speaker, "text": text,
            "options": opts, "next_key": next_key,
            "end_action": end_action, "end_scene": end_scene,
            "folder": "Awakening"
        }

    return dialogues

# ============================================================
# GENERATE FILES
# ============================================================

def generate():
    dialogues = define_all_dialogues()

    # Generate GUIDs for all nodes
    guids = {}
    for key in dialogues:
        # Use key + "appetite2024" as seed for deterministic but unique GUID
        guids[key] = make_guid(key + "appetite2024salt")

    # Create folder structure
    folders = set(d["folder"] for d in dialogues.values())
    for folder in folders:
        folder_path = os.path.join(BASE_DIR, folder)
        os.makedirs(folder_path, exist_ok=True)

    # Generate .asset and .meta files
    count = 0
    for key, d in dialogues.items():
        folder = d["folder"]
        file_path = os.path.join(BASE_DIR, folder, key + ".asset")

        # Resolve next_node GUID
        next_guid = None
        if d["next_key"] and d["next_key"] in guids:
            next_guid = guids[d["next_key"]]

        # Resolve option next_node GUIDs
        options = None
        if d["options"]:
            options = []
            for opt in d["options"]:
                opt_next_guid = ""
                if len(opt) > 1 and opt[1] in guids:
                    opt_next_guid = guids[opt[1]]
                options.append({
                    "text": opt[0],
                    "next_guid": opt_next_guid,
                    "hungerChange": opt[2] if len(opt) > 2 else 0
                })

        asset_content = make_asset(
            name=d["name"],
            text=d["text"],
            speaker=d["speaker"],
            options=options,
            next_node_guid=next_guid,
            end_action=d["end_action"],
            end_action_scene=d["end_scene"]
        )

        meta_content = make_meta(guids[key])

        with open(file_path, "w", encoding="utf-8") as f:
            f.write(asset_content)

        with open(file_path + ".meta", "w", encoding="utf-8") as f:
            f.write(meta_content)

        count += 1
        print(f"  [{folder}] {key}.asset")

    print(f"\n✅ Generated {count} dialogue assets in {len(folders)} folders.")

    # Print summary of special nodes
    print("\n--- Scene Transitions ---")
    for key, d in dialogues.items():
        if d["end_action"] != "None":
            print(f"  {key} → {d['end_action']}: {d['end_scene']}")

    print("\n--- Branch Nodes ---")
    for key, d in dialogues.items():
        if d["options"] and len(d["options"]) > 1:
            print(f"  {key}: {len(d['options'])} options")

    return guids, dialogues

if __name__ == "__main__":
    generate()
