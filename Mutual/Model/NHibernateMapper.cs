using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mutual.Helpers.NHibernate;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Impl;
using NHibernate.Tool.hbm2ddl;

namespace Mutual.Model
{
	public static class NHibernateMapper
	{
		private static HbmMapping _mapping;

		public static HbmMapping Mapping
		{
			get
			{
				if (_mapping != null) return _mapping;

				// Игнорируемые классы
				var ignoreTypes = new Type[0]; // new []{ typeof(SqliteConnection) };

				var mapper = new ConventionModelMapper();
				var modelInspector = (IModelInspector) new SimpleModelInspector();
				var simpleModelInspector = (SimpleModelInspector) mapper.ModelInspector;

				// Игнорируем property помеченные атрибутами Ignore
				simpleModelInspector.IsPersistentProperty((m, declared) => {
					if (declared)
						return true;
					return modelInspector.IsPersistentProperty(m)
						&& m.GetCustomAttributes(typeof(IgnoreAttribute), false).Length == 0;
				});

				simpleModelInspector.IsRootEntity((type, declared) => {
					return declared
						|| (type.IsClass
							&& type.BaseType != null

							//если наследуемся от класса который не маплен то это простое наследование
							&& (typeof(object) == type.BaseType || !modelInspector.IsEntity(type.BaseType)))
						&& modelInspector.IsEntity(type);
				});

				simpleModelInspector.IsPersistentId((m, declared) => {
					if (declared)
						return true;
					return m.Name.Equals("Id");
				});

				mapper.BeforeMapClass += (inspector, type, customizer) => {
					if (type.GetProperty("Id")?.PropertyType == typeof(Guid)) {
						customizer.Id(m => m.Generator(Generators.Guid));
					} else {
						customizer.Id(m => m.Generator(Generators.Native));
					}
				};

				mapper.BeforeMapManyToMany += (inspector, member, customizer) => {
					customizer.ForeignKey("none");
				};

				mapper.BeforeMapBag += (inspector, member, customizer) => {
					customizer.Key(k => {
						k.Column(member.GetContainerEntity(inspector).Name + "Id");
						k.ForeignKey("none");
					});
				};

				mapper.BeforeMapManyToOne += (inspector, member, customizer) => {
					var propertyInfo = (PropertyInfo) member.LocalMember;
					customizer.Column(member.LocalMember.Name + "Id");
					customizer.ForeignKey("none");
				};


				mapper.Class<Department>(m => {
					m.Table("dep");
					m.Id(x => x.Id,  
						x => {
							x.Column("KeyId");
							x.Generator(Generators.Sequence, 
								p => p.Params(new Dictionary<string, object>() {
									{ "sequence", "s_dep" }
								})
							);
						});
					m.Property(x => x.InStatus, c => c.Column("in_status"));
					m.Property(x => x.OutStatus, c => c.Column("out_status"));
					m.Property(x => x.Name, c => c.Column("text"));
					m.Property(x => x.ShortName, c => c.Column("stext"));
					m.Property(x => x.StatusDep, c => c.Column("status_dep"));
				});
				
				mapper.Class<Patient>(m => {
					m.Id(x => x.Id,  
						x => {
							x.Column("KeyId");
							x.Generator(Generators.Sequence, 
								p => p.Params(new Dictionary<string, object>() {
									{ "sequence", "s_patient" }
								})
							);
						});
				});
				
				mapper.Class<Visit>(m => {
					m.Id(x => x.Id,  
						x => {
							x.Column("KeyId");
							x.Generator(Generators.Sequence, 
								p => p.Params(new Dictionary<string, object>() {
									{ "sequence", "s_visit" }
								})
							);
						});
					m.ManyToOne(x => x.Patient, c => c.Column("PatientId"));
					m.ManyToOne(x => x.Comming, c => {
						c.Column("RootId");
						c.Lazy(LazyRelation.NoLazy);
						c.NotFound(NotFoundMode.Ignore);
					});
				
					m.ManyToOne(x => x.DepartmentIn, c => c.Column("DepId"));
					m.ManyToOne(x => x.DepartmentOut, c => c.Column("Dep1Id"));
					m.ManyToOne(x => x.DepartmentProfile, c => c.Column("DepProfId"));
					m.Property(x => x.FromDate, c => c.Column("dat"));
					m.Property(x => x.ToDate, c => c.Column("dat1"));
				});

				var assembly = Assembly.GetExecutingAssembly();

				var types = assembly.GetTypes()
					.Where(t => t.Namespace != null && t.Namespace.StartsWith("Mutual.Model"))
					.Where(t => !t.IsAbstract
						&& !t.IsInterface
						&& (t.GetProperty("Id") != null || t.GetProperty("KeyId") != null))
					.Except(ignoreTypes)
					.Distinct();

				_mapping = mapper.CompileMappingFor(types);
				return _mapping;
			}
		}
	}
}