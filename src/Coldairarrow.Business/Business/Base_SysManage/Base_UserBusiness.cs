using AutoMapper;
using Coldairarrow.Business.Cache;
using Coldairarrow.Entity.Base_SysManage;
using Coldairarrow.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;

namespace Coldairarrow.Business.Base_SysManage
{
    public class Base_UserBusiness : BaseBusiness<Base_User>, IBase_UserBusiness, IDependency
    {
        #region DI

        public Base_UserBusiness(IBase_UserDTOCache sysUserCache, IOperator @operator, IPermissionManage permissionManage)
        {
            _sysUserCache = sysUserCache;
            _operator = @operator;
            _permissionManage = permissionManage;
        }
        IBase_UserDTOCache _sysUserCache { get; }
        IOperator _operator { get; }
        IPermissionManage _permissionManage { get; }

        #endregion

        #region ��д

        protected override EnumType.LogType LogType => EnumType.LogType.ϵͳ�û�����;

        #endregion

        #region �ⲿ�ӿ�

        public List<Base_UserDTO> GetDataList(Pagination pagination, string userId = null, string keyword = null)
        {
            var where = LinqHelper.True<Base_User>();
            if (!userId.IsNullOrEmpty())
                where = where.And(x => x.Id == userId);
            if (!keyword.IsNullOrEmpty())
            {
                where = where.And(x =>
                    x.UserName.Contains(keyword)
                    || x.RealName.Contains(keyword));
            }

            var list = GetIQueryable().Where(where).GetPagination(pagination).ToList()
                .Select(x => Mapper.Map<Base_UserDTO>(x))
                .ToList();

            SetProperty(list);

            return list;

            void SetProperty(List<Base_UserDTO> users)
            {
                //�����û���ɫ����
                List<string> userIds = users.Select(x => x.Id).ToList();
                var userRoles = (from a in Service.GetIQueryable<Base_UserRoleMap>()
                                 join b in Service.GetIQueryable<Base_SysRole>() on a.RoleId equals b.Id
                                 where userIds.Contains(a.UserId)
                                 select new
                                 {
                                     a.UserId,
                                     RoleId = b.Id,
                                     b.RoleName
                                 }).ToList();
                users.ForEach(aUser =>
                {
                    var roleList = userRoles.Where(x => x.UserId == aUser.Id);
                    aUser.RoleIdList = roleList.Select(x => x.RoleId).ToList();
                    aUser.RoleNameList = roleList.Select(x => x.RoleName).ToList();
                });
            }
        }

        public Base_User GetTheData(string id)
        {
            return GetEntity(id);
        }

        public Base_UserDTO GetTheInfo(string userId)
        {
            return _sysUserCache.GetCache(userId);
        }

        [DataAddLog(EnumType.LogType.ϵͳ�û�����, "�û�", "RealName")]
        [DataRepeatValidate(
            new string[] { "UserName" },
            new string[] { "�û���" })]
        public void AddData(Base_User newData)
        {
            Insert(newData);
        }

        [DataEditLog(EnumType.LogType.ϵͳ�û�����, "�û�", "RealName")]
        [DataRepeatValidate(
            new string[] { "UserName" },
            new string[] { "�û���" })]
        public void UpdateData(Base_User theData)
        {
            if (theData.Id == "Admin" && _operator.UserId != theData.Id)
                throw new Exception("��ֹ���ĳ�������Ա��");

            Update(theData);
            _sysUserCache.UpdateCache(theData.Id);
        }

        [DataDeleteLog(EnumType.LogType.ϵͳ�û�����, "�û�", "RealName")]
        public void DeleteData(List<string> ids)
        {
            var adminUser = GetTheInfo("Admin");
            if (ids.Contains(adminUser.Id))
                throw new Exception("��������Ա�������˺�,��ֹɾ����");
            var userIds = GetIQueryable().Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToList();

            Delete(ids);
            _sysUserCache.UpdateCache(userIds);
        }

        public void SetUserRole(string userId, List<string> roleIds)
        {
            Service.Delete_Sql<Base_UserRoleMap>(x => x.UserId == userId);
            var insertList = roleIds.Select(x => new Base_UserRoleMap
            {
                Id = IdHelper.GetId(),
                UserId = userId,
                RoleId = x
            }).ToList();

            Service.Insert(insertList);
            _sysUserCache.UpdateCache(userId);
            _permissionManage.UpdateUserPermissionCache(userId);
        }

        public List<string> GetUserRoleIds(string userId)
        {
            return GetTheInfo(userId).RoleIdList;
        }

        public AjaxResult ChangePwd(string oldPwd, string newPwd)
        {
            AjaxResult res = new AjaxResult() { Success = true };
            string userId = _operator.UserId;
            oldPwd = oldPwd.ToMD5String();
            newPwd = newPwd.ToMD5String();
            var theUser = GetIQueryable().Where(x => x.Id == userId && x.Password == oldPwd).FirstOrDefault();
            if (theUser == null)
            {
                res.Success = false;
                res.Msg = "ԭ���벻��ȷ��";
            }
            else
            {
                theUser.Password = newPwd;
                Update(theUser);
            }

            _sysUserCache.UpdateCache(userId);

            return res;
        }

        #endregion

        #region ˽�г�Ա

        #endregion

        #region ����ģ��

        #endregion
    }
}