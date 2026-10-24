# Uri 弹窗通知插件

通过外部 URI 调用在 ClassIsland 上显示弹窗提醒。

## 这是什么？

一个 ClassIsland 插件。让你用一行网址或Win+R就能在 ClassIsland 上弹出提醒。

比如在浏览器地址/Win+R栏输入：

`classisland://plugins/notify?msg=上课了`
>ClassIsland 主界面就会弹出"上课了"的提醒。

## 使用须知

**在使用该插件前，请先确认你的Classisland已注册了Uri协议（通用-基本-注册Uri协议-检查是否打开）**

## 新手教程

**注册Uri后，本插件即可正常运行**

**首先介绍几个简单的指令**

`classisland://plugins/notify?maskMsg=上课了`
>（会显示持续5秒的遮罩：上课了）

`classisland://plugins/notify?msg=请同学们回到座位`
>（会显示持续5秒“请同学们回到座位”）

`classisland://plugins/notify?path=D:/文本.txt`
>（其会显示存在D盘根目录下名叫“文本.txt”的文件内容）

`classisland://plugins/notify?maskMsg=通知&msg=今天下午由于下雨，课间操取消`
>（会先显示 5 秒遮罩“通知”，然后显示 5 秒正文“今天下午由于下雨，课间操取消”）

`classisland://plugins/notify?id=daily_tips`
>（会读取系统临时文件夹 %TEMP%\ClassIslandNotify_daily_tips.txt 的内容并显示。适合外部程序动态生成内容）

**滚动显示用法**
`classisland://plugins/notify?mode=rolling&msg=这是一段非常长的文本，会从右向左循环滚动显示，直到时间结束`
>（正文会变成滚动模式。默认在 20 秒内滚动 2 遍）

`classisland://plugins/notify?mode=rolling&msg=自定义滚动速度&scrollspeed=2`
>（正文滚动，2 倍速。会根据文字长度自动计算所需时长，滚动 2 遍）

`classisland://plugins/notify?mode=rolling&msg=固定总时长&duration=30&time=3`
>（正文滚动，总共显示 30 秒，在这 30 秒内正好滚完 3 遍。注：一旦指定 duration，scrollSpeed 会失效）

**时间与语音控制**
`classisland://plugins/notify?maskMsg=紧急通知&maskDuration=10`
>（遮罩“紧急通知”会显示 10 秒，而不是默认的 5 秒）

`classisland://plugins/notify?msg=大家早上好&speech=1`
>（显示正文“大家早上好”5 秒，同时会通过语音播报出来。也可以写成 speech=true）

---
---

## 高等教学

### 短格式

所有通过Uri来唤醒提醒都 **需要含有前缀“classisland://plugins/notify?"**

其后可以任接以下参数 **（采用”&“连接，=赋值）**

| 参数名 | 默认值 | 可设置项A(参数意义) | 可设置项B(参数意义) | 备注 |
|---|---|---|---|---|
| mode | text | text(静态文本) | rolling(滚动文本) | |
| path | | 文件完整路径 | | **注意，路径用 "/" 代替 "\"** |
| id | | 文件名 | | 其会访问 `%TEMP%\ClassIslandNotify_文件名.txt` |
| speech | false | true/1(语音朗读正文) | false/0(静默通知) | |
| maskMsg | | 遮罩内容 | | |
| maskDuration | 5 | 秒 | | |
| msg | | 正文内容 | | |
| duration | 5(text)/20(rolling) | 秒 | | |
| scrollspeed | | 文字滚动速度(rolling) | | |
| time | 2 | 次(显示次数) | | |

**优先级**

path>id>msg

duration>scrollspeed

### 文件内容、组格式

**经作者测试:目前能在UTF-8编码中稳定运行**

#### 通用
Uri触发格式：
`classisland://plugins/notify?path=（使用文件的完整路径）`
>例如：“classisland://plugins/notify?path=C:/user/username/Desktop/测试.txt”访问的是username桌面上的"测试.txt"
`classisland://plugins/notify?id=(使用%TEMP%文件夹内的文件名)&maskDuration=5&maskMsg=遮罩文案`
>例如：“classisland://plugins/notify?id=测试”访问的是"%TEMP%\ClassIslandNotify_测试.txt"

#### 文件内容
可在文件内部预先配置好要显示的内容（文字可以显示一行或两行）
>在.txt内直接换行，classisland上就会显示多行

#### 组格式
- 组格式按照以下格式识别 **（Num填入数字）**
```text
text{Num}(
    mode:{text/rolling}
    maskDuration{Num}Msg:{"遮罩内容"}
    duration{Num}scrollspeed{Num}time{Num}msg:{"正文内容"}
    speech:{false/true}
)
```
**注意事项：当同时含有duration与scrollspeed时，将会以duration作为实际参数**
**组文件中maskDuration不赋默认值**

- 例如以下为一个完整可执行的组文件：
```text
text{1}(
    maskDuration{3}Msg:{"仅3s遮罩"}
)
text{2}(
    mode:{text}
    duration{5}msg:{"显示5s的静默静态文本"}
)
text{3}(
    mode:{rolling}
    maskDuration{5}Msg:{"显示5s的遮罩"}
    msg:{"滚动2遍共20s的说话文本"}
    speech:{1}
)
text{4}(
    mode:{rolling}
    maskDuration{6}Msg:{"显示6s的遮罩"}
    scrollspeed{2}time{1}msg:{"显示一遍80*2px/s的滚动静默文本"}
    speech:{false}
)
text{5}(
    mode:{rolling}
    maskDuration{6}Msg:{"显示6s的遮罩"}
    duration{10}time{1}msg:{"显示一遍10s的静默文本"}
    speech:{true}
)
```

>如遇到任何插件问题，欢迎与作者沟通讨论
>测试环境：Classisland版本：2.1.0.1
>测试设备：希沃Windows10 x86

>图标来源：牛图标 by Dave Gandy on [Icon-Icons.com](https://icon-icons.com/zh/authors/370-dave-gandy)