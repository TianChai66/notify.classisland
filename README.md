# Uri 弹窗通知插件

通过外部 URI 调用在 ClassIsland 上显示弹窗提醒。

## 这是什么？

一个 ClassIsland 插件。让你用一行网址或 Win+R 就能在 ClassIsland 上弹出提醒。

比如在浏览器地址栏或 Win+R 栏输入：

```text
classisland://plugins/notify?正文=上课了
```
```text
classisland://plugins/notify?msg=上课了
```

>ClassIsland 主界面就会弹出“上课了”的提醒。

## 使用须知

**在使用该插件前，请先确认你的 ClassIsland 已注册了 URI 协议（通用-基本-注册 URI 协议-检查是否打开）**

---

## 新手教程

**注册 URI 后，本插件即可正常运行。**

### 小白专属：纯中文极简格式

> **注**：为了降低使用门槛，本插件全面支持纯中文参数。全半角标点、双引号、结尾括号均可省略！

**1. 中文短格式（Win+R或浏览器直接触发）：**

```text
classisland://plugins/notify?正文=同学们上课了
```
>（会在屏幕中央显示 5 秒“同学们上课了”）

```text
classisland://plugins/notify?遮罩=通知&正文=今天下午由于下雨，课间操取消
```
>（会先显示 5 秒遮罩“通知”，然后显示 5 秒正文）

**2. 中文组格式（写在 TXT 文件里）：**

> **⚠️ 注意**：中文组格式必须在行首加 % 号，以防止普通的 TXT 文章被误判。同一行内的参数才属于同一条设置。

```text
%通知1：
    模式：{滚动}
    遮罩时长{3}遮罩：{重要通知}
    速度{2}次数{2}正文：{这是一段不需要打引号和结尾括号的纯中文滚动文本}
```

将上述内容保存为 test.txt（**注意编码必须是 UTF-8**），然后用以下链接触发：

```text
classisland://plugins/notify?路径=D:/test.txt
```

---

## 进阶：标准英文指令

**首先介绍几个简单的指令：**

```text
classisland://plugins/notify?maskMsg=上课了
```
>（会显示持续 5 秒的遮罩：上课了）

```text
classisland://plugins/notify?msg=请同学们回到座位
```
>（会显示持续 5 秒“请同学们回到座位”）

```text
classisland://plugins/notify?path=D:/文本.txt
```
>（其会显示存在 D 盘根目录下名叫“文本.txt”的文件内容）

```text
classisland://plugins/notify?maskMsg=通知&msg=今天下午由于下雨，课间操取消
```
>（会先显示 5 秒遮罩“通知”，然后显示 5 秒正文“今天下午由于下雨，课间操取消”）

```text
classisland://plugins/notify?id=daily_tips
```
>（会读取系统临时文件夹 %TEMP%\ClassIslandNotify_daily_tips.txt 的内容并显示。适合外部程序动态生成内容）

**滚动显示用法：**

```text
classisland://plugins/notify?mode=rolling&msg=这是一段非常长的文本，会从右向左循环滚动显示，直到时间结束
```
>（正文会变成滚动模式。默认在 20 秒内滚动 2 遍）

```text
classisland://plugins/notify?mode=rolling&msg=自定义滚动速度&scrollspeed=2
```
>（正文滚动，2 倍速。会根据文字长度自动计算所需时长，滚动 2 遍）

```text
classisland://plugins/notify?mode=rolling&msg=固定总时长&duration=30&time=3
```
>（正文滚动，总共显示 30 秒，在这 30 秒内正好滚完 3 遍。注：一旦指定 duration，scrollspeed 会失效）

**时间控制：**

```text
classisland://plugins/notify?maskMsg=紧急通知&maskDuration=10
```
>（遮罩“紧急通知”会显示 10 秒，而不是默认的 5 秒）

---

## 高等教学

### 短格式

所有通过 URI 来唤醒提醒都 **需要含有前缀“classisland://plugins/notify?”**

其后可以任接以下参数 **（采用“&”连接，=赋值，支持中英混搭）**

| 英文参数名 | 中文参数名 | 默认值 | 可设置项A(参数意义) | 可设置项B(参数意义) | 备注 |
|---|---|---|---|---|---|
| mode | 模式 | text | text(静态文本) | rolling(滚动文本) | |
| path | 路径 | | 文件完整路径 | | **注意，路径用 "/" 代替 "\"** |
| id | 编号 | | 文件名 | | 其会访问 %TEMP%\ClassIslandNotify_文件名.txt |
| maskMsg | 遮罩 | | 遮罩内容 | | |
| maskDuration | 遮罩时长 | 5 | 秒 | | |
| msg | 正文 | | 正文内容 | | |
| duration | 时长 | 5(text)/20(rolling) | 秒 | | |
| scrollspeed | 速度 | | 文字滚动速度(rolling) | | |
| time | 次数 | 2 | 次(显示次数) | | |

**优先级**

path > id > msg

duration > scrollspeed

### 文件内容、组格式

**经作者测试：目前能在 UTF-8 编码中稳定运行**

#### 通用

URI 触发格式：
```text
classisland://plugins/notify?path=（使用文件的完整路径）
```
>例如：classisland://plugins/notify?path=C:/user/username/Desktop/测试.txt 访问的是 username 桌面上的“测试.txt”

```text
classisland://plugins/notify?id=(使用%TEMP%文件夹内的文件名)&maskDuration=5&maskMsg=遮罩文案
```
>例如：classisland://plugins/notify?id=测试 访问的是 %TEMP%\ClassIslandNotify_测试.txt

#### 文件内容
可在文件内部预先配置好要显示的内容（文字可以显示一行或两行）
>在 .txt 内直接换行，ClassIsland 上就会显示多行

#### 组格式

**支持中文极简与英文标准两种写法（中英混搭亦可）**。严格遵循**同行绑定**原则（时间/速度等参数必须和对应的文字在同一行）。

*注意事项：当同时含有 duration 与 scrollspeed 时，将会以 duration 作为实际参数*
*注意事项：组文件中 maskDuration 不赋默认值*

---

**【方案一：中文极简格式】**

> **⚠️ 必须在行首加 %，以防止普通的 TXT 文章被误判。**

例如以下为一个完整可执行的中文组文件（包含全角符号与无引号测试）：

```text
%通知1：
    模式：{静态}
    遮罩时长{3}遮罩：{第一组纯中文}
    时长{5}正文：{这是第一组，不打引号和括号也能完美运行！}

%通知2：
    模式：{滚动}
    遮罩时长{4}遮罩：{中英混搭也支持}
    速度{2}次数{2}正文：{这是第二组，包含了中文冒号：以及各种符号}
```

---

**【方案二：英文标准格式】**

```text
text{Num}(
    mode:{text/rolling}
    maskDuration{Num}Msg:{"遮罩内容"}
    duration{Num}scrollspeed{Num}time{Num}msg:{"正文内容"}
)
```

例如以下为一个完整可执行的英文组文件：

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
    msg:{"滚动2遍共20s的静默文本"}
)
text{4}(
    mode:{rolling}
    maskDuration{6}Msg:{"显示6s的遮罩"}
    scrollspeed{2}time{1}msg:{"显示一遍80*2px/s的滚动静默文本"}
)
text{5}(
    mode:{rolling}
    maskDuration{6}Msg:{"显示6s的遮罩"}
    duration{10}time{1}msg:{"显示一遍10s的静默文本"}
)
```

---

## 进阶联动：配合 ClassIsland 自动化使用

你完全不需要记那些复杂的路径！你可以利用 ClassIsland 自带的**“自动化”**功能，实现到点自动播报。

1. 打开 **ClassIsland 设置** -> **自动化**，新建一个工作流。
2. 添加一个**行动**，选择官方的 **“运行”** -> **“打开 URI”**。
3. 在 URI 框中粘贴你的指令，例如：

```text
classisland://plugins/notify?路径=C:/Users/你的用户名/Desktop/测试.txt
```

4. 添加一个**触发器**（比如：上课事件）。

这样，每当上课时，ClassIsland 就会自动去读取你的文件并大字播报啦！

---

> 如遇到任何插件问题，欢迎与作者沟通讨论
> **测试环境**：ClassIsland 版本：2.1.0.1
> **测试设备**：希沃 Windows10 x64

> 图标来源：牛图标 by Dave Gandy on [Icon-Icons.com](https://icon-icons.com/zh/authors/370-dave-gandy)
