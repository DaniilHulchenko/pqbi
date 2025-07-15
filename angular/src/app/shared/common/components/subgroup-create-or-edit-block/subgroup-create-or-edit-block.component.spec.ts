import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubgroupCreateOrEditBlockComponent } from './subgroup-create-or-edit-block.component';

describe('SubgroupCreateOrEditBlockComponent', () => {
  let component: SubgroupCreateOrEditBlockComponent;
  let fixture: ComponentFixture<SubgroupCreateOrEditBlockComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubgroupCreateOrEditBlockComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SubgroupCreateOrEditBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
