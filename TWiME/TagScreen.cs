﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Extensions;

namespace TWiME {
    public class TagScreen {
        private List<Window> _windowList = new List<Window>();

        public List<Window> windows {
            get { return _windowList; }
        }

        private ILayout layout;
        private readonly Monitor _parent;
        private readonly int _tag;
        public int activeLayout;

        public int tag {
            get { return _tag; }
        }

        public Monitor parent {
            get { return _parent; }
        }

        public TagScreen(Monitor parent, int tag) {
            activeLayout =
                Manager.GetLayoutIndexFromName(Manager.settings.ReadSettingOrDefault("DefaultLayout",
                                                                                     parent.Screen.DeviceName.Replace(
                                                                                         ".", ""), tag.ToString(),
                                                                                     "DefaultLayout"));
            _parent = parent;
            _tag = tag;
            initLayout();
            Manager.WindowCreate += Manager_WindowCreate;
            Manager.WindowDestroy += Manager_WindowDestroy;
        }

        public void initLayout() {
            if (!Manager.settings.readOnly) {
                Manager.settings.AddSetting(Manager.GetLayoutNameFromIndex(activeLayout),
                                            parent.Screen.DeviceName.Replace(".", ""), _tag.ToString(), "DefaultLayout");
            }
            Layout instance =
                (Layout)
                Activator.CreateInstance(Manager.layouts[activeLayout],
                                         new object[] {_windowList, _parent.Controlled, this});
            layout = instance;
        }

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetForegroundWindow();

        private void Manager_WindowDestroy(object sender, WindowEventArgs args) {
            Window newWindow = (Window) sender;
            IEnumerable<Window> deleteList =
                (from window in _windowList where window.handle == newWindow.handle select window);
            if (deleteList.Count() > 0) {
                Window toRemove = deleteList.First();
                _windowList.Remove(toRemove);
                Manager.Log("Removing window: {0} {1}".With(toRemove.ClassName, toRemove.Title), 1);
                layout.UpdateWindowList(_windowList);
                if (parent.EnabledTag == tag) {
                    layout.Assert();
                }
            }
        }

        private void Manager_WindowCreate(object sender, WindowEventArgs args) {
            bool rulesThisMonitor = false, rulesThisTag = false;
            int stackPosition =
                Convert.ToInt32(Manager.settings.ReadSettingOrDefault(0, "General.Windows.DefaultStackPosition"));
            foreach (KeyValuePair<WindowMatch, WindowRule> keyValuePair in Manager.windowRules) {
                if (keyValuePair.Key.windowMatches((Window) sender)) {
                    if (keyValuePair.Value.rule == WindowRules.monitor) {
                        if (Manager.monitors[keyValuePair.Value.data].Name == _parent.Name) {
                            rulesThisMonitor = true;
                        }
                        else {
                            return;
                        }
                    }
                    if (keyValuePair.Value.rule == WindowRules.tag) {
                        if (keyValuePair.Value.data - 1 == _tag) {
                            //-1 because tag 1 is index 0, etc
                            rulesThisTag = true;
                        }
                        else {
                            return;
                        }
                    }
                    if (keyValuePair.Value.rule == WindowRules.stack) {
                        stackPosition = keyValuePair.Value.data;
                    }
                }
            }
            if ((args.monitor.DeviceName == _parent.Name || rulesThisMonitor) &&
                (_parent.EnabledTag == _tag || rulesThisTag)) {
                Window newWindow = (Window) sender;
                if (stackPosition < 0) {
                    stackPosition = _windowList.Count() - stackPosition;
                    if (stackPosition < 0) {
                        stackPosition = 0;
                    }
                }
                if (stackPosition >= _windowList.Count) {
                    stackPosition = _windowList.Count;
                }

                _windowList.Insert(stackPosition, newWindow);
                Manager.Log("Adding Window: " + newWindow.ClassName + " " + newWindow, 1);
                layout.UpdateWindowList(_windowList);
                layout.Assert();
            }
        }

        ~TagScreen() {
            foreach (Window window in _windowList) {
                window.Visible = true;
                window.Maximised = false;
            }
        }

        private int getFocusedWindowIndex() {
            IntPtr hWnd = GetForegroundWindow();
            for (int i = 0; i < _windowList.Count; i++) {
                if (_windowList[i].handle == hWnd) {
                    return i;
                }
            }
            return -1;
        }

        public void catchMessage(HotkeyMessage message) {
            if (message.level == Level.screen) {
                if (message.message == Message.Focus) {
                    if (_windowList.Count == 0) {
                        return;
                    }
                    if (Screen.FromHandle(message.handle).DeviceName == _parent.Name) {
                        int newIndex = getFocusedWindowIndex() + message.data;
                        if (newIndex >= _windowList.Count) {
                            newIndex = 0;
                        }
                        else if (newIndex < 0) {
                            newIndex = _windowList.Count - 1;
                        }
                        Console.WriteLine(newIndex);
                        _windowList[newIndex].Activate();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.Switch) {
                    if (_windowList.Count == 0) {
                        return;
                    }
                    if (Screen.FromHandle(message.handle).DeviceName == _parent.Name) {
                        int oldIndex = getFocusedWindowIndex();
                        int newIndex = oldIndex + message.data;
                        if (newIndex >= _windowList.Count) {
                            newIndex = 0;
                        }
                        else if (newIndex < 0) {
                            newIndex = _windowList.Count - 1;
                        }
                        Window oldWindow = _windowList[oldIndex];
                        Window newWindow = _windowList[newIndex];
                        _windowList[oldIndex] = newWindow;
                        _windowList[newIndex] = oldWindow;
                        layout.Assert();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.SwitchThis) {
                    int oldIndex = getFocusedWindowIndex();
                    if (oldIndex == -1) {
                        oldIndex = 0;
                    }
                    int newIndex = message.data;
                    Window newWindow, oldWindow;
                    try {
                        oldWindow = _windowList[oldIndex];
                        newWindow = _windowList[newIndex];
                    }
                    catch (ArgumentOutOfRangeException) {
                        return;
                    }
                    _windowList[oldIndex] = newWindow;
                    _windowList[newIndex] = oldWindow;
                    layout.Assert();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.FocusThis) {
                    if (message.data < _windowList.Count) {
                        _windowList[message.data].Activate();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.Monitor) {
                    int newMonitorIndex = Manager.GetFocussedMonitorIndex() + message.data;
                    if (newMonitorIndex < 0) {
                        newMonitorIndex = Manager.monitors.Count - 1;
                    }
                    else if (newMonitorIndex >= Manager.monitors.Count) {
                        newMonitorIndex = 0;
                    }
                    Monitor newMonitor = Manager.monitors[newMonitorIndex];
                    Window focussedWindow = getFocusedWindow();
                    newMonitor.CatchWindow(this.throwWindow(focussedWindow));
                    layout.Assert();
                    newMonitor.GetActiveScreen().enable();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.MonitorMoveThis) {
                    Manager.monitors[message.data].CatchWindow(throwWindow(getFocusedWindow()));
                    layout.Assert();
                    Manager.monitors[message.data].GetActiveScreen().Activate();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.Splitter) {
                    layout.MoveSplitter(message.data / 100.0f);
                    Manager.settings.AddSetting(layout.GetSplitter(), parent.Screen.DeviceName.Replace(".", ""),
                                                _tag.ToString(), "Splitter");
                }
                if (message.message == Message.VSplitter) {
                    layout.MoveSplitter(message.data / 100.0f, true);
                    Manager.settings.AddSetting(layout.GetSplitter(true), parent.Screen.DeviceName.Replace(".", ""),
                                                _tag.ToString(), "VSplitter");
                }
                if (message.message == Message.Close) {
                    foreach (Window window in _windowList) {
                        window.Visible = true;
                        window.Maximised = false;
                    }
                }
                if (message.message == Message.Close) {
                    _windowList[message.data].Close();
                }
            }
            else {
                getFocusedWindow().CatchMessage(message);
                layout.Assert();
            }
        }

        public Window throwWindow(Window window) {
            _windowList.Remove(window);
            return window;
        }

        public Window getFocusedWindow() {
            int index = getFocusedWindowIndex();
            if (index == -1) {
                return new Window("", parent.Bar.Handle, "", "", true);
            }
            return _windowList[index];
        }

        public void Activate() {
            if (_windowList.Count > 0) {
                _windowList[0].Activate();
            }
            else {
                new Window("", _parent.Bar.Handle, "", "", true).Activate();
            }
        }

        public void CatchWindow(Window window) {
            _windowList.Add(window);
        }

        public Image getStateImage(Size previewSize) {
            return layout.StateImage(previewSize);
        }

        public void disable() {
            foreach (Window window in windows) {
                window.Visible = false;
            }
        }

        public void disable(TagScreen swappingWith) {
            foreach (Window window in windows) {
                if (!swappingWith.windows.Contains(window)) {
                    window.Visible = false;
                }
            }
        }

        public void enable() {
            foreach (Window window in windows) {
                window.Visible = true;
            }
            layout.Assert();
        }

        public void assertLayout() {
            layout.Assert();
        }
    }
}