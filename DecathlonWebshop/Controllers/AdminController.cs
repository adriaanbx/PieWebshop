﻿using DecathlonWebshop.Auth;
using DecathlonWebshop.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DecathlonWebshop.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        #region UserManagement
        public IActionResult UserManagement()
        {
            var users = _userManager.Users;
            return View(users);
        }

        public IActionResult AddUser(ApplicationUser user)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(AddUserViewModel addUserViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(addUserViewModel);
            }

            var user = new ApplicationUser()
            {
                UserName = addUserViewModel.UserName,
                Email = addUserViewModel.Email,
                Birthdate = addUserViewModel.Birthdate,
                City = addUserViewModel.City,
                Country = addUserViewModel.Country
            };

            IdentityResult result = await _userManager.CreateAsync(user, addUserViewModel.Password);

            if (result.Succeeded)
            {
                return RedirectToAction("UserManagement", _userManager.Users);
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(addUserViewModel);
        }
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return RedirectToAction("UserManagement", _userManager.Users);

            //TODO werkt wel maar ik zou verwachten dat de claimsprincipal van de login hier ook verschijnt, of zijn het enkel de claims die niet toegewezen zijn aan een claimsprincipal?
            var userClaims = await _userManager.GetClaimsAsync(user);

            var viewModel = new EditUserViewModel() { Id = user.Id, Email = user.Email, UserName = user.UserName, 
                IsEnabled = user.IsEnabled, Birthdate = user.Birthdate, City = user.City, Country = user.Country, UserClaims = userClaims.Select(c => c.Value).ToList() };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel editUserViewModel)
        {
            var user = await _userManager.FindByIdAsync(editUserViewModel.Id);

            if (user != null)
            {
                user.Email = editUserViewModel.Email;
                user.IsEnabled = editUserViewModel.IsEnabled;
                user.UserName = editUserViewModel.UserName;
                user.Birthdate = editUserViewModel.Birthdate;
                user.City = editUserViewModel.City;
                user.Country = editUserViewModel.Country;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                    return RedirectToAction("UserManagement", _userManager.Users);

                ModelState.AddModelError("", "User not updated, something went wrong.");

                return View(editUserViewModel);
            }

            return RedirectToAction("UserManagement", _userManager.Users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                IdentityResult result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                    return RedirectToAction("UserManagement");
                else
                    ModelState.AddModelError("", "Something went wrong while deleting this user.");
            }
            else
            {
                ModelState.AddModelError("", "This user can't be found");
            }
            return View("UserManagement", _userManager.Users);
        }
        #endregion

        #region RoleManagement

        public IActionResult RoleManagement()
        {
            var roles = _roleManager.Roles;
            return View(roles);
        }

        public IActionResult AddNewRole() => View();

        [HttpPost]
        public async Task<IActionResult> AddNewRole(AddRoleViewModel addRoleViewModel)
        {

            if (!ModelState.IsValid) return View(addRoleViewModel);

            var role = new IdentityRole
            {
                Name = addRoleViewModel.RoleName
            };

            IdentityResult result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                return RedirectToAction("RoleManagement", _roleManager.Roles);
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(addRoleViewModel);
        }

        public async Task<IActionResult> EditRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return RedirectToAction("RoleManagement", _roleManager.Roles);

            var editRoleViewModel = new EditRoleViewModel
            {
                Id = role.Id,
                RoleName = role.Name,
                Users = new List<string>()
            };

            foreach (var user in _userManager.Users.ToList())
            {
                //TODO InvalidOperationException: There is already an open DataReader associated with this Connection which must be closed first.
                if (await _userManager.IsInRoleAsync(user, role.Name))
                    editRoleViewModel.Users.Add(user.UserName);
            }

            return View(editRoleViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditRole(EditRoleViewModel editRoleViewModel)
        {
            var role = await _roleManager.FindByIdAsync(editRoleViewModel.Id);

            if (role != null)
            {
                role.Name = editRoleViewModel.RoleName;

                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                    return RedirectToAction("RoleManagement", _roleManager.Roles);

                ModelState.AddModelError("", "Role not updated, something went wrong.");

                return View(editRoleViewModel);
            }

            return RedirectToAction("RoleManagement", _roleManager.Roles);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(string id)
        {
            IdentityRole role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("RoleManagement", _roleManager.Roles);
                ModelState.AddModelError("", "Something went wrong while deleting this role.");
            }
            else
            {
                ModelState.AddModelError("", "This role can't be found.");
            }
            return View("RoleManagement", _roleManager.Roles);
        }

        //Users in roles

        public async Task<IActionResult> AddUserToRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
                return RedirectToAction("RoleManagement", _roleManager.Roles);

            var addUserToRoleViewModel = new UserRoleViewModel { RoleId = role.Id };

            foreach (var user in _userManager.Users.ToList())
            {
                if (!await _userManager.IsInRoleAsync(user, role.Name))
                {
                    addUserToRoleViewModel.Users.Add(user);
                }
            }

            return View(addUserToRoleViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddUserToRole(UserRoleViewModel userRoleViewModel)
        {
            var user = await _userManager.FindByIdAsync(userRoleViewModel.UserId);
            var role = await _roleManager.FindByIdAsync(userRoleViewModel.RoleId);

            var result = await _userManager.AddToRoleAsync(user, role.Name);

            if (result.Succeeded)
            {
                return RedirectToAction("RoleManagement", _roleManager.Roles);
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(userRoleViewModel);
        }

        public async Task<IActionResult> DeleteUserFromRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
                return RedirectToAction("RoleManagement", _roleManager.Roles);

            var addUserToRoleViewModel = new UserRoleViewModel { RoleId = role.Id };

            foreach (var user in _userManager.Users)
            {
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    addUserToRoleViewModel.Users.Add(user);
                }
            }

            return View(addUserToRoleViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUserFromRole(UserRoleViewModel userRoleViewModel)
        {
            var user = await _userManager.FindByIdAsync(userRoleViewModel.UserId);
            var role = await _roleManager.FindByIdAsync(userRoleViewModel.RoleId);

            var result = await _userManager.RemoveFromRoleAsync(user, role.Name);

            if (result.Succeeded)
            {
                return RedirectToAction("RoleManagement", _roleManager.Roles);
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(userRoleViewModel);
        }
        #endregion

        #region ClaimsManagement
        public async Task<IActionResult> ManageClaimsForUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction("UserManagement", _userManager.Users);

            var claimsManagementViewModel = new ClaimsManagementViewModel { UserId = user.Id, AllClaimsList = DecathlonWebshopClaimTypes.ClaimsList };

            return View(claimsManagementViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ManageClaimsForUser(ClaimsManagementViewModel claimsManagementViewModel)
        {
            var user = await _userManager.FindByIdAsync(claimsManagementViewModel.UserId);

            if (user == null)
                return RedirectToAction("UserManagement", _userManager.Users);

            Claim claim = new Claim(claimsManagementViewModel.ClaimId, claimsManagementViewModel.ClaimId);

            var resultClaim = await _userManager.AddClaimAsync(user, claim);

            if (resultClaim.Succeeded)
            {
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                    return RedirectToAction("UserManagement", _userManager.Users);

                ModelState.AddModelError("", "User not updated, something went wrong.");
            }
            else
            {
                ModelState.AddModelError("", "The claim was not added to the user");
            }

            return View(claimsManagementViewModel);
        }
    }
    #endregion
}

