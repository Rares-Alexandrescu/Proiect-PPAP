import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditAccount } from './edit-account';

describe('EditAccount', () => {
  let component: EditAccount;
  let fixture: ComponentFixture<EditAccount>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditAccount],
    }).compileComponents();

    fixture = TestBed.createComponent(EditAccount);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
