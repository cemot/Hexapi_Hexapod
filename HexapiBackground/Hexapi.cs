﻿using System.Diagnostics;
using System.Threading.Tasks;
using HexapiBackground.Enums;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace HexapiBackground{
    internal sealed class Hexapi
    {
        private readonly InverseKinematics _ik;
        private readonly XboxController _xboxController;

        private bool _isMovementStarted = false;

        private double _legLiftHeight = 30;
        private double _nomGaitSpeed = 60;
        private SelectedFunction _selectedFunction = SelectedFunction.GaitSpeed;

        private double _travelLengthX; //Current Travel length X
        private double _travelLengthZ; //Current Travel length Z
        private double _travelRotationY; //Current Travel Rotation Y

        private GaitType _gaitType = GaitType.TripleTripod12Steps;
        private double _bodyPosY = 65;
        private double _bodyRotX1;
        private double _bodyRotZ1;
        private double _bodyPosX;
        private double _bodyPosZ;

        internal Hexapi()
        {
            _ik = new InverseKinematics();

            _xboxController = new XboxController();
            _xboxController.Open();

            _xboxController.LeftDirectionChanged += XboxControllerLeftDirectionChanged;
            _xboxController.RightDirectionChanged += XboxControllerRightDirectionChanged;
            _xboxController.DpadDirectionChanged += XboxController_DpadDirectionChanged;
            _xboxController.LeftTriggerChanged += XboxController_LeftTriggerChanged;
            _xboxController.RightTriggerChanged += XboxController_RightTriggerChanged;
            _xboxController.FunctionButtonChanged += XboxControllerFunctionButtonChanged;
            _xboxController.BumperButtonChanged += XboxController_BumperButtonChanged;
        }

        #region Main logic loop 

        public void Start()
        {
            Task.Factory.StartNew(() =>
            { _ik.Start(); }, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region XBox 360 Controller related...

        //4 = Left bumper, 5 = Right bumper
        private void XboxController_BumperButtonChanged(int button)
        {
            switch (_selectedFunction)
            {
                case SelectedFunction.TranslateHorizontal:
                case SelectedFunction.Translate3D:
                case SelectedFunction.GaitSpeed: //A
                    if (button == 5)
                    {
                        if (_nomGaitSpeed < 200)
                        {
                            _nomGaitSpeed = _nomGaitSpeed + 5;
                        }
                    }
                    else
                    {
                        if (_nomGaitSpeed > 20)
                        {
                            _nomGaitSpeed = _nomGaitSpeed - 5;
                        }
                    }
                    break;
                case SelectedFunction.LegHeight: //B
                    if (button == 5)
                    {
                        if (_legLiftHeight < 160)
                            _legLiftHeight = _legLiftHeight + 5;
                    }
                    else
                    {
                        if (_legLiftHeight > 30)
                            _legLiftHeight = _legLiftHeight - 5;
                    }
                    break;
            }

            _ik.RequestSetGaitOptions(_nomGaitSpeed, _legLiftHeight);
        }


        private void XboxControllerFunctionButtonChanged(int button)
        {
            switch (button)
            {
                case 0: //A
                    _selectedFunction = SelectedFunction.GaitSpeed;
                    break;
                case 1: //B
                    _selectedFunction = SelectedFunction.LegHeight;
                    break;
                case 2: //X
                    _selectedFunction = SelectedFunction.TranslateHorizontal;
                    break;
                case 3: //Y
                    _selectedFunction = SelectedFunction.Translate3D;
                    break;
                case 7: //Start button
                    _isMovementStarted = !_isMovementStarted;

                    _ik.RequestSetMovement(_isMovementStarted);

                    Debug.WriteLine("setting movement to  " + _isMovementStarted);
                    break;
                case 6: //back button
                    AvController.SaveWaypoint();
                    break;
                default:
                    Debug.WriteLine("button? " + button);
                    break;
            }
        }

        private void XboxController_RightTriggerChanged(int trigger)
        {
            _travelLengthX = Helpers.Map(trigger, 0, 10000, 0, 70);
            _ik.RequestMovement(_nomGaitSpeed, _travelLengthX, _travelLengthZ, _travelRotationY);
        }

        private void XboxController_LeftTriggerChanged(int trigger)
        {
            _travelLengthX = -Helpers.Map(trigger, 0, 10000, 0, 70);
            _ik.RequestMovement(_nomGaitSpeed, _travelLengthX, _travelLengthZ, _travelRotationY);
        }


        private void XboxController_DpadDirectionChanged(ControllerVector sender)
        {
            switch (sender.Direction)
            {
                case ControllerDirection.Left:
                    if ((int) _gaitType > 0)
                    {
                        _gaitType--;
                        _ik.RequestSetGaitType(_gaitType);
                    }
                    break;
                case ControllerDirection.Right:
                    if ((int) _gaitType < 4)
                    {
                        _gaitType++;
                        _ik.RequestSetGaitType(_gaitType);
                    }
                    break;
                case ControllerDirection.Up:
                    if (_bodyPosY < 120)
                    {
                        _bodyPosY = _bodyPosY + 5;
                        _ik.RequestBodyPosition(_bodyRotX1, _bodyRotZ1, _bodyPosX, _bodyPosZ, _bodyPosY);
                    }
                    break;
                case ControllerDirection.Down:
                    if (_bodyPosY > 35)
                    {
                        _bodyPosY = _bodyPosY - 5;
                        _ik.RequestBodyPosition(_bodyRotX1, _bodyRotZ1, _bodyPosX, _bodyPosZ, _bodyPosY);
                    }
                    break;
            }
        }

        private void XboxControllerRightDirectionChanged(ControllerVector sender)
        {
            switch (sender.Direction)
            {
                case ControllerDirection.Left:
                    _travelRotationY = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 3);
                    _travelLengthZ = 0;
                    break;
                case ControllerDirection.UpLeft:
                    _travelRotationY = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 3);
                    _travelLengthZ = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 90);
                    break;
                case ControllerDirection.DownLeft:
                    _travelRotationY = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 3);
                    _travelLengthZ = Helpers.Map(sender.Magnitude, 0, 10000, 0, 90);
                    break;
                case ControllerDirection.Right:
                    _travelRotationY = Helpers.Map(sender.Magnitude, 0, 10000, 0, 3);
                    _travelLengthZ = 0;
                    break;
                case ControllerDirection.UpRight:
                    _travelRotationY = Helpers.Map(sender.Magnitude, 0, 10000, 0, 3);
                    _travelLengthZ = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 90);
                    break;
                case ControllerDirection.DownRight:
                    _travelRotationY = Helpers.Map(sender.Magnitude, 0, 10000, 0, 3);
                    _travelLengthZ = Helpers.Map(sender.Magnitude, 0, 10000, 0, 90);
                    break;
                case ControllerDirection.Up:
                    _travelLengthZ = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 160);
                    _travelRotationY = 0;
                    break;
                case ControllerDirection.Down:
                    _travelLengthZ = Helpers.Map(sender.Magnitude, 0, 10000, 0, 160);
                    _travelRotationY = 0;
                    break;
            }

            _ik.RequestMovement(_nomGaitSpeed, _travelLengthX, _travelLengthZ, _travelRotationY);
        }

        private void SetBodyRot(ControllerVector sender)
        {
            switch (sender.Direction)
            {
                case ControllerDirection.Left:
                    _bodyRotX1 = 0;
                    _bodyRotZ1 = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    break;
                case ControllerDirection.UpLeft:
                    _bodyRotX1 = Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    _bodyRotZ1 = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    break;
                case ControllerDirection.UpRight:
                    _bodyRotX1 = Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    _bodyRotZ1 = Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    break;
                case ControllerDirection.Right:
                    _bodyRotX1 = 0;
                    _bodyRotZ1 = Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    break;
                case ControllerDirection.Up:
                    _bodyRotX1 = Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    _bodyRotZ1 = 0;

                    break;
                case ControllerDirection.Down:
                    _bodyRotX1 = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    _bodyRotZ1 = 0;
                    break;
                case ControllerDirection.DownLeft:
                    _bodyRotZ1 = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    _bodyRotX1 = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    break;
                case ControllerDirection.DownRight:
                    _bodyRotZ1 = Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    _bodyRotX1 = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 7);
                    break;
            }

            _ik.RequestBodyPosition(_bodyRotX1, _bodyRotZ1, _bodyPosX, _bodyPosZ, _bodyPosY);
        }

        private void SetBodyRotOffset(ControllerVector sender)
        {
            switch (sender.Direction)
            {
                case ControllerDirection.Left:
                    _bodyPosX = 0;
                    _bodyPosZ = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    break;
                case ControllerDirection.UpLeft:
                    _bodyPosX = Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    _bodyPosZ = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    break;
                case ControllerDirection.UpRight:
                    _bodyPosX = Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    _bodyPosZ = Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    break;
                case ControllerDirection.Right:
                    _bodyPosX = 0;
                    _bodyPosZ = Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    break;
                case ControllerDirection.Up:
                    _bodyPosX = Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    _bodyPosZ = 0;
                    break;
                case ControllerDirection.Down:
                    _bodyPosX = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    _bodyPosZ = 0;
                    break;
                case ControllerDirection.DownLeft:
                    _bodyPosZ = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    _bodyPosX = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    break;
                case ControllerDirection.DownRight:
                    _bodyPosZ = Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    _bodyPosX = -Helpers.Map(sender.Magnitude, 0, 10000, 0, 40);
                    break;
            }

            _ik.RequestBodyPosition(_bodyRotX1, _bodyRotZ1, _bodyPosX, _bodyPosZ, _bodyPosY);
        }

        private void XboxControllerLeftDirectionChanged(ControllerVector sender)
        {
            switch (_selectedFunction)
            {
                case SelectedFunction.TranslateHorizontal:
                    SetBodyRotOffset(sender);
                    break;
                case SelectedFunction.Translate3D:
                    SetBodyRot(sender);
                    break;
                default:
                    SetBodyRot(sender);
                    break;
            }
        }

        #endregion
    }
}