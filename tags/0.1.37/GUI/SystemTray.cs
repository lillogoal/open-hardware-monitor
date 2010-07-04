﻿/*
  
  Version: MPL 1.1/GPL 2.0/LGPL 2.1

  The contents of this file are subject to the Mozilla Public License Version
  1.1 (the "License"); you may not use this file except in compliance with
  the License. You may obtain a copy of the License at
 
  http://www.mozilla.org/MPL/

  Software distributed under the License is distributed on an "AS IS" basis,
  WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
  for the specific language governing rights and limitations under the License.

  The Original Code is the Open Hardware Monitor code.

  The Initial Developer of the Original Code is 
  Michael Möller <m.moeller@gmx.ch>.
  Portions created by the Initial Developer are Copyright (C) 2009-2010
  the Initial Developer. All Rights Reserved.

  Contributor(s):

  Alternatively, the contents of this file may be used under the terms of
  either the GNU General Public License Version 2 or later (the "GPL"), or
  the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
  in which case the provisions of the GPL or the LGPL are applicable instead
  of those above. If you wish to allow use of your version of this file only
  under the terms of either the GPL or the LGPL, and not to allow others to
  use your version of this file under the terms of the MPL, indicate your
  decision by deleting the provisions above and replace them with the notice
  and other provisions required by the GPL or the LGPL. If you do not delete
  the provisions above, a recipient may use your version of this file under
  the terms of any one of the MPL, the GPL or the LGPL.
 
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI {
  public class SystemTray : IDisposable {
    private IComputer computer;
    private List<SensorNotifyIcon> list = new List<SensorNotifyIcon>();
    private bool mainIconEnabled = false;
    private NotifyIcon mainIcon;

    public SystemTray(IComputer computer) {
      this.computer = computer;
      computer.HardwareAdded += new HardwareEventHandler(HardwareAdded);
      computer.HardwareRemoved += new HardwareEventHandler(HardwareRemoved);

      this.mainIcon = new NotifyIcon();

      ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
      ToolStripMenuItem hideShowItem = new ToolStripMenuItem("Hide/Show");
      hideShowItem.Click += delegate(object obj, EventArgs args) {
        SendHideShowCommand();
      };
      contextMenuStrip.Items.Add(hideShowItem);
      contextMenuStrip.Items.Add(new ToolStripSeparator());      
      ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
      exitItem.Click += delegate(object obj, EventArgs args) {
        SendExitCommand();
      };
      contextMenuStrip.Items.Add(exitItem);
      this.mainIcon.ContextMenuStrip = contextMenuStrip;
      this.mainIcon.DoubleClick += delegate(object obj, EventArgs args) {
        SendHideShowCommand();
      };
      this.mainIcon.Icon = EmbeddedResources.GetIcon("smallicon.ico");
    }

    private void HardwareRemoved(IHardware hardware) {
      hardware.SensorAdded -= new SensorEventHandler(SensorAdded);
      hardware.SensorRemoved -= new SensorEventHandler(SensorRemoved);
      foreach (ISensor sensor in hardware.Sensors) 
        SensorRemoved(sensor);
      foreach (IHardware subHardware in hardware.SubHardware)
        HardwareRemoved(subHardware);
    }

    private void HardwareAdded(IHardware hardware) {
      foreach (ISensor sensor in hardware.Sensors)
        SensorAdded(sensor);
      hardware.SensorAdded += new SensorEventHandler(SensorAdded);
      hardware.SensorRemoved += new SensorEventHandler(SensorRemoved);
      foreach (IHardware subHardware in hardware.SubHardware)
        HardwareAdded(subHardware);
    }

    private void SensorAdded(ISensor sensor) {
      if (Config.Get(new Identifier(sensor.Identifier, 
        "tray").ToString(), false)) 
        Add(sensor, false);   
    }

    private void SensorRemoved(ISensor sensor) {
      if (Contains(sensor)) 
        Remove(sensor, false);
    }

    public void Dispose() {
      foreach (SensorNotifyIcon icon in list)
        icon.Dispose();
      mainIcon.Dispose();
    }

    public void Redraw() {
      foreach (SensorNotifyIcon icon in list)
        icon.Update();
    }

    public bool Contains(ISensor sensor) {
      foreach (SensorNotifyIcon icon in list)
        if (icon.Sensor == sensor)
          return true;
      return false;
    }

    public void Add(ISensor sensor, bool balloonTip) {
      if (Contains(sensor)) {
        return;
      } else {        
        list.Add(new SensorNotifyIcon(this, sensor, balloonTip));
        UpdateMainIconVisibilty();
        Config.Set(new Identifier(sensor.Identifier, "tray").ToString(), true);
      }
    }

    public void Remove(ISensor sensor) {
      Remove(sensor, true);
    }

    private void Remove(ISensor sensor, bool deleteConfig) {
      if (deleteConfig) {
        Config.Remove(
          new Identifier(sensor.Identifier, "tray").ToString());
        Config.Remove(
          new Identifier(sensor.Identifier, "traycolor").ToString());
      }
      SensorNotifyIcon instance = null;
      foreach (SensorNotifyIcon icon in list)
        if (icon.Sensor == sensor)
          instance = icon;
      if (instance != null) {
        list.Remove(instance);
        UpdateMainIconVisibilty();
        instance.Dispose();        
      }
    }

    public event EventHandler HideShowCommand;

    public void SendHideShowCommand() {
      if (HideShowCommand != null)
        HideShowCommand(this, null);
    }

    public event EventHandler ExitCommand;

    public void SendExitCommand() {
      if (ExitCommand != null)
        ExitCommand(this, null);
    }

    private void UpdateMainIconVisibilty() {
      if (mainIconEnabled) {
        mainIcon.Visible = list.Count == 0;
      } else {
        mainIcon.Visible = false;
      }
    }

    public bool IsMainIconEnabled {
      get { return mainIconEnabled; }
      set {
        if (mainIconEnabled != value) {
          mainIconEnabled = value;
          UpdateMainIconVisibilty();
        }
      }
    }
  }
}
